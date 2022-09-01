using System.Collections.Generic;
using Dapper;

namespace DatabaseSchemaReport.DAL
{
    public class TableRepository : BaseRepository
    {
        public IEnumerable<dynamic> FindAllTableNameAndDescription()
        {
            return Execute(conn =>
                conn.Query(@"select TABLE_NAME as TableName, Description
                                from INFORMATION_SCHEMA.TABLES as t
                                left join (select sys.objects.name as TableName, ep.value as Description, type_desc as TypeDesc, is_ms_shipped as IsMsShipped
                                           from sys.objects
                                           outer apply fn_listextendedproperty(default,'SCHEMA', schema_name(schema_id),'TABLE', name, null, null) ep
                                           where sys.objects.name not in ('sysdiagrams')
                                ) as d on d.TableName = t.TABLE_NAME
                                where t.TABLE_TYPE = 'BASE TABLE' and TypeDesc = 'USER_TABLE' and IsMsShipped = '0'
                                order by TABLE_NAME")
            );
        }

        public IEnumerable<dynamic> ExecSpHelpIndex(string tableName)
        {
            return Execute(conn =>
                conn.Query(@"EXEC sp_helpindex @tableName", new { tableName })
            );
        }

        public IEnumerable<dynamic> ExecSpFkeys(string tableName)
        {
            return Execute(conn =>
                conn.Query(@"EXEC sp_fkeys @tableName", new { tableName })
            );
        }
    }
}