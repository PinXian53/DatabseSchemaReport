using System.Collections.Generic;
using System.Linq;
using Dapper;

namespace DatabaseSchemaReport.DAL
{
    public class DatabaseRepository : BaseRepository
    {
        public IEnumerable<string> FindAllDatabaseName()
        {
            return Execute(conn =>
                // 排除系統預設資料庫
                conn.Query(@"select name from sys.databases 
                                where name not in ('master', 'tempdb', 'model', 'msdb', 'distribution') 
                                order by name")
                    .Select(o => (string)o.name)
            );
        }
    }
}