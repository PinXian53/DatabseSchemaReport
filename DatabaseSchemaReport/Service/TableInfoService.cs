using System.Collections.Generic;
using DatabaseSchemaReport.DAL;

namespace DatabaseSchemaReport.Service
{
    public class TableInfoService
    {
        private readonly TableRepository _tableRepository = new TableRepository();

        public IEnumerable<dynamic> FindAllTableNameAndDescription()
        {
            return _tableRepository.FindAllTableNameAndDescription();
        }
    }
}