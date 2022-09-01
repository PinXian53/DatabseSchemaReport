using System.Collections.Generic;
using DatabaseSchemaReport.DAL;

namespace DatabaseSchemaReport.Service
{
    public class DatabaseInfoService
    {
        private readonly DatabaseRepository _databaseRepository = new DatabaseRepository();

        public IEnumerable<string> FindAllDatabaseName()
        {
            return _databaseRepository.FindAllDatabaseName();
        }
    }
}