namespace DatabaseSchemaReport.Model
{
    public class ColumnInfoDto
    {
        public int OrdinalPosition { set; get; }
        public string ColumnName { set; get; }
        public string DataType { set; get; }
        public int? CharacterMaximumLength { set; get; }
        public bool IsNullable { set; get; }
        public bool IsPk { set; get; }
        public bool IsFk { set; get; }
        public string ColumnDefault { set; get; }
        public string Description { set; get; }
    }
}