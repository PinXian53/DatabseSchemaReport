using System;
using System.Collections.Generic;
using System.Linq;
using DatabaseSchemaReport.DAL;
using DatabaseSchemaReport.Model;

namespace DatabaseSchemaReport.Service
{
    public class ColumnInfoService
    {
        private readonly ColumnRepository _columnRepository = new ColumnRepository();
        private readonly TableRepository _tableRepository = new TableRepository();

        public IEnumerable<ColumnInfoDto> FindAllColumnInfo(string tableName)
        {
            var dbColumnInfo = _columnRepository.FindAllColumnInfo(tableName);
            var dbIndex = _tableRepository.ExecSpHelpIndex(tableName);
            var dbFk = _tableRepository.ExecSpFkeys(tableName);

            var indexColName = new HashSet<string>();
            foreach (var db in dbIndex)
            {
                foreach (var colName in ((string)db.index_keys).Split(new[] { ", " }, StringSplitOptions.None))
                {
                    indexColName.Add(colName);
                }
            }

            var fkColName = new HashSet<string>();
            foreach (var db in dbFk)
            {
                foreach (var colName in ((string)db.PKCOLUMN_NAME).Split(new[] { ", " }, StringSplitOptions.None))
                {
                    fkColName.Add(colName);
                }
            }

            return dbColumnInfo.Select(db => new ColumnInfoDto
            {
                OrdinalPosition = db.ordinalPosition,
                ColumnName = db.columnName,
                DataType = db.dataType,
                CharacterMaximumLength = db.characterMaximnmLength,
                IsNullable = db.isNullable == "YES",
                IsPk = indexColName.Contains((string)db.columnName),
                IsFk = fkColName.Contains((string)db.columnName),
                ColumnDefault = db.columnDefault,
                Description = db.description
            }).ToList();
        }
    }
}