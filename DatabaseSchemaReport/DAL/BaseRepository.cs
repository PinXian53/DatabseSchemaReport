using System;
using System.Data.SqlClient;
using DatabaseSchemaReport.Util;

namespace DatabaseSchemaReport.DAL
{
    public class BaseRepository
    {
        private const string DbError = "資料庫異常";

        protected T Execute<T>(Func<SqlConnection, T> func)
        {
            try
            {
                using (var connection = new SqlConnection(GetConnectString()))
                {
                    return func(connection);
                }
            }
            catch (Exception)
            {
                throw new ApplicationException(DbError);
            }
        }

        private static string GetConnectString()
        {
            return SqlServerConnectStringUtil.GetDataBaseConnectionString();
        }
    }
}