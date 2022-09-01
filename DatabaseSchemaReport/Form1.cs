using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using DatabaseSchemaReport.Constant;
using DatabaseSchemaReport.Model;
using DatabaseSchemaReport.Service;
using DatabaseSchemaReport.Util;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace DatabaseSchemaReport
{
    public partial class Form1 : Form
    {
        private readonly DatabaseInfoService _databaseInfoService = new DatabaseInfoService();
        private readonly TableInfoService _tableInfoService = new TableInfoService();
        private readonly ColumnInfoService _columnInfoService = new ColumnInfoService();

        public Form1()
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            InitializeComponent();
            Init();
        }

        private void Init()
        {
            cbExportFormat.Items.Clear();
            cbExportFormat.Items.Add(ExportFormat.SingleSheet);
            cbExportFormat.Items.Add(ExportFormat.MultiSheet);
            cbExportFormat.SelectedIndex = 0;
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            try
            {
                Cursor = Cursors.WaitCursor;

                cbDataBase.Items.Clear();
                lbConnect.Text = string.Empty;

                SqlServerConnectStringUtil.Ip = txtServerIp.Text.Trim();
                SqlServerConnectStringUtil.Account = txtAccount.Text.Trim();
                SqlServerConnectStringUtil.Password = txtPassword.Text.Trim();
                SqlServerConnectStringUtil.DatabaseName = "master"; // 指定使用 master 資料庫
                var databaseNames = _databaseInfoService.FindAllDatabaseName();

                cbDataBase.Items.Add("請選擇資料庫"); // 預設顯示
                cbDataBase.SelectedIndex = 0;
                foreach (var databaseName in databaseNames)
                {
                    cbDataBase.Items.Add(databaseName);
                }

                lbConnect.Text = "連線成功，請從下方選擇資料庫!";
                lbConnect.ForeColor = Color.Green;
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void cbDataBase_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                Cursor = Cursors.WaitCursor;

                lvTable.Clear();

                if (cbDataBase.SelectedIndex == 0)
                {
                    return;
                }

                SqlServerConnectStringUtil.DatabaseName = cbDataBase.Text;

                lvTable.Columns.Add("", 40);
                lvTable.Columns.Add("Table Name", 200);
                lvTable.Columns.Add("Description", 290);

                var allTableNameAndDescription = _tableInfoService.FindAllTableNameAndDescription();

                lvTable.BeginUpdate();
                foreach (var tableNameAndDescription in allTableNameAndDescription)
                {
                    string tableName = tableNameAndDescription.TableName;
                    string description = tableNameAndDescription.Description;
                    var lvi = new ListViewItem();
                    lvi.SubItems.Add(tableName);
                    lvi.SubItems.Add(description);
                    lvTable.Items.Add(lvi);
                }

                lvTable.EndUpdate();
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void btnSelectAll_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in lvTable.Items)
            {
                item.Checked = true;
            }
        }

        private void btnSelectInvert_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in lvTable.Items)
            {
                item.Checked = false;
            }
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            var tableNameAmdDescriptions =
            (
                from ListViewItem item in lvTable.Items
                where item.Checked
                select new TableNameAndDescriptionDto
                    {TableName = item.SubItems[1].Text, TableDescription = item.SubItems[2].Text}
            ).ToList();

            if (tableNameAmdDescriptions.Count == 0)
            {
                MessageBox.Show(this, "未勾選任何表格", "錯誤");
                return;
            }

            ExportReport(tableNameAmdDescriptions);
        }

        private async void ExportReport(IReadOnlyCollection<TableNameAndDescriptionDto> tableNameAmdDescriptions)
        {
            try
            {
                string txtFolderPath;
                using (var folderBrowserDialog = new FolderBrowserDialog())
                {
                    var result = folderBrowserDialog.ShowDialog();
                    if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(folderBrowserDialog.SelectedPath))
                    {
                        txtFolderPath = folderBrowserDialog.SelectedPath;
                    }
                    else
                    {
                        return;
                    }
                }

                Cursor = Cursors.WaitCursor;
                btnExport.Enabled = false;
                cbDataBase.Enabled = false;
                btnConnect.Enabled = false;

                var fileName = Path.Combine(txtFolderPath,
                    "DatabseSchemaReport(" + SqlServerConnectStringUtil.DatabaseName + ").xlsx");

                var exportFormat = ExportFormat.SingleSheet.ToString() == cbExportFormat.Text
                    ? ExportFormat.SingleSheet
                    : ExportFormat.MultiSheet;

                await Task.Run(() =>
                {
                    if (ExportFormat.SingleSheet == exportFormat)
                    {
                        ExportSingleSheet(fileName, tableNameAmdDescriptions);
                    }
                    else
                    {
                        ExportMultiSheet(fileName, tableNameAmdDescriptions);
                    }
                });

                MessageBox.Show(this, "匯出成功，匯出路徑：" + fileName, "成功");
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                throw;
            }
            finally
            {
                Cursor = Cursors.Default;
                btnExport.Enabled = true;
                cbDataBase.Enabled = true;
                btnConnect.Enabled = true;
            }
        }

        private void ExportSingleSheet(string fileName,
            IReadOnlyCollection<TableNameAndDescriptionDto> tableNameAmdDescriptions)
        {
            var count = 1;
            var row = 1;
            const string sheetName = "schema";
            var excelPackage = new ExcelPackage();
            excelPackage.Workbook.Worksheets.Add(sheetName);
            var sheet = excelPackage.Workbook.Worksheets[sheetName];

            foreach (var tableNameAmdDescription in tableNameAmdDescriptions)
            {
                var tableName = tableNameAmdDescription.TableName;
                var tableDescription = tableNameAmdDescription.TableDescription;

                var columnInfoDtos =
                    _columnInfoService.FindAllColumnInfo(tableNameAmdDescription.TableName);

                var title = count + ". " + tableName;

                SetTitle(row, sheet, title);
                row++;
                SetTableTitle(row, sheet, tableName, tableDescription);
                row += 2;
                SetTableData(row, sheet, columnInfoDtos);
                row += columnInfoDtos.Count();
                sheet.Cells.AutoFitColumns();
                // 留一行空白
                row++;
                
                count++;
            }

            excelPackage.SaveAs(new FileInfo(fileName));
        }

        private void ExportMultiSheet(string fileName,
            IReadOnlyCollection<TableNameAndDescriptionDto> tableNameAmdDescriptions)
        {
            var count = 1;
            var excelPackage = new ExcelPackage();

            foreach (var tableNameAmdDescription in tableNameAmdDescriptions)
            {
                var tableName = tableNameAmdDescription.TableName;
                var tableDescription = tableNameAmdDescription.TableDescription;

                var columnInfoDtos =
                    _columnInfoService.FindAllColumnInfo(tableNameAmdDescription.TableName);

                // excel sheetName 有長度限制，故超過 30 就先截斷
                var sheetName = count + ". " + tableName;
                if (sheetName.Length > 30)
                {
                    sheetName = sheetName.Substring(0, 30);
                }

                excelPackage.Workbook.Worksheets.Add(sheetName);

                var sheet = excelPackage.Workbook.Worksheets[sheetName];
                SetTableTitle(1, sheet, tableName, tableDescription);
                SetTableData(3, sheet, columnInfoDtos);
                sheet.Cells.AutoFitColumns();

                count++;
            }

            excelPackage.SaveAs(new FileInfo(fileName));
        }

        private void SetTitle(int row, ExcelWorksheet sheet, string title)
        {
            sheet.Cells[row, 1].Value = title;
        }

        private void SetTableTitle(int row, ExcelWorksheet sheet, string tableName, string tableDescription)
        {
            sheet.Cells[row, 1, row, 2].Merge = true;
            sheet.Cells[row, 1].Value = "資料表名稱";
            SetBorder(sheet.Cells[row, 1, row, 2]);
            SetBackgroundColor(sheet.Cells[row, 1, row, 2]);
            sheet.Cells[row, 3, row, 4].Merge = true;
            sheet.Cells[row, 3].Value = tableName;
            SetBorder(sheet.Cells[row, 3, row, 4]);
            SetBackgroundColor(sheet.Cells[row, 3, row, 4]);
            sheet.Cells[row, 5, row, 6].Merge = true;
            sheet.Cells[row, 5].Value = "資料表描述";
            SetBorder(sheet.Cells[row, 5, row, 6]);
            SetBackgroundColor(sheet.Cells[row, 5, row, 6]);
            sheet.Cells[row, 7, row, 8].Merge = true;
            sheet.Cells[row, 7].Value = tableDescription;
            SetBorder(sheet.Cells[row, 7, row, 8]);
            SetBackgroundColor(sheet.Cells[row, 7, row, 8]);

            row++;

            sheet.Cells[row, 1].Value = "SN";
            SetBorder(sheet.Cells[row, 1]);
            SetBackgroundColor(sheet.Cells[row, 1]);
            sheet.Cells[row, 2, row, 3].Merge = true;
            sheet.Cells[row, 2].Value = "欄位";
            SetBorder(sheet.Cells[row, 2, row, 3]);
            SetBackgroundColor(sheet.Cells[row, 2, row, 3]);
            sheet.Cells[row, 4].Value = "資料型態";
            SetBorder(sheet.Cells[row, 4]);
            SetBackgroundColor(sheet.Cells[row, 4]);
            sheet.Cells[row, 5].Value = "長度";
            SetBorder(sheet.Cells[row, 5]);
            SetBackgroundColor(sheet.Cells[row, 5]);
            sheet.Cells[row, 6].Value = "null";
            SetBorder(sheet.Cells[row, 6]);
            SetBackgroundColor(sheet.Cells[row, 6]);
            sheet.Cells[row, 7].Value = "PK";
            SetBorder(sheet.Cells[row, 7]);
            SetBackgroundColor(sheet.Cells[row, 7]);
            sheet.Cells[row, 8].Value = "描述";
            SetBorder(sheet.Cells[row, 8]);
            SetBackgroundColor(sheet.Cells[row, 8]);
        }

        private void SetTableData(int row, ExcelWorksheet sheet, IEnumerable<ColumnInfoDto> columnInfoDtos)
        {
            foreach (var columnInfoDto in columnInfoDtos.OrderBy(o => o.OrdinalPosition))
            {
                sheet.Cells[row, 1].Value = columnInfoDto.OrdinalPosition.ToString();
                SetBorder(sheet.Cells[row, 1]);
                sheet.Cells[row, 2, row, 3].Merge = true;
                sheet.Cells[row, 2].Value = columnInfoDto.ColumnName;
                SetBorder(sheet.Cells[row, 2, row, 3]);
                sheet.Cells[row, 4].Value = columnInfoDto.DataType;
                SetBorder(sheet.Cells[row, 4]);
                sheet.Cells[row, 5].Value = GetLength(columnInfoDto.DataType, columnInfoDto.CharacterMaximumLength);
                SetBorder(sheet.Cells[row, 5]);
                sheet.Cells[row, 6].Value = columnInfoDto.IsNullable ? "FALSE" : "TRUE";
                SetBorder(sheet.Cells[row, 6]);
                sheet.Cells[row, 7].Value = (columnInfoDto.IsPk ? "PK" : "") +
                                            (columnInfoDto.IsPk && columnInfoDto.IsFk ? ", " : "") +
                                            (columnInfoDto.IsFk ? "FK" : "");
                SetBorder(sheet.Cells[row, 7]);
                sheet.Cells[row, 8].Value = columnInfoDto.Description;
                SetBorder(sheet.Cells[row, 8]);
                row++;
            }
        }

        private static void SetBorder(ExcelRange excelRange)
        {
            excelRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            excelRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            excelRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            excelRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
        }

        private static void SetBackgroundColor(ExcelRange excelRange)
        {
            excelRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
            excelRange.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255, 255, 153));
        }

        /**
         * 客製化長度
         */
        private static string GetLength(string dataType, int? characterMaximumLength)
        {
            switch (dataType)
            {
                case "int":
                    return "4";
                case "datetimeoffset":
                    return "10";
                case "bit":
                    return "1";
                case "nvarchar":
                    return characterMaximumLength == -1 ? "MAX" : characterMaximumLength.ToString();
                default:
                    return characterMaximumLength == null ? string.Empty : characterMaximumLength.ToString();
            }
        }

        private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            string dllName = args.Name.Contains(",")
                ? args.Name.Substring(0, args.Name.IndexOf(','))
                : args.Name.Replace(".dll", "");
            dllName = dllName.Replace(".", "_");
            if (dllName.EndsWith("_resources")) return null;
            System.Resources.ResourceManager rm =
                new System.Resources.ResourceManager(GetType().Namespace + ".Properties.Resources",
                    Assembly.GetExecutingAssembly());
            byte[] bytes = (byte[]) rm.GetObject(dllName);
            return Assembly.Load(bytes);
        }
    }
}