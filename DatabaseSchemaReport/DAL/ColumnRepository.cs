using System.Collections.Generic;
using Dapper;

namespace DatabaseSchemaReport.DAL
{
    public class ColumnRepository : BaseRepository
    {
        public IEnumerable<dynamic> FindAllColumnInfo(string tableName)
        {
            return Execute(conn =>
                conn.Query(@"select 
                        cc.ORDINAL_POSITION as ordinalPosition,
                        cc.COLUMN_NAME as columnName,
                        cc.DATA_TYPE as dataType,
                        cc.CHARACTER_MAXIMUM_LENGTH as characterMaximnmLength,
                        cc.IS_NULLABLE as isNullable,
                        cc.COLUMN_DEFAULT as columnDefault,
                        sep.value as description
                        from sys.tables st
                        inner join sys.columns sc on st.object_id = sc.object_id
                        left join sys.extended_properties sep on st.object_id = sep.major_id
                        and sc.column_id = sep.minor_id
                        and sep.name = 'MS_Description'
                        left join (select * from  INFORMATION_SCHEMA.COLUMNS where TABLE_NAME = @tableName) cc on cc.COLUMN_NAME = sc.name
                        where st.name = @tableName", new { tableName })
            );
        }
    }
}