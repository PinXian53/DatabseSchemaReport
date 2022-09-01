namespace DatabaseSchemaReport.Util
{
    public static class SqlServerConnectStringUtil
    {
        public static string Ip { set; get; }
        public static string Account { set; get; }
        public static string Password { set; get; }
        public static string DatabaseName { set; get; }

        public static string GetDataBaseConnectionString()
        {
            return $"Data Source={Ip};Initial Catalog={DatabaseName};User ID={Account};PWD={Password};" +
                   "Integrated Security=false;MultipleActiveResultSets=true";
        }
    }
}