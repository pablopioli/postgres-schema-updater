using System.Collections.Generic;
using System.Linq;

namespace Postgres.SchemaUpdater
{
    public class TableCatalog
    {
        private Dictionary<string, List<TableDefinition>> _schemas = new Dictionary<string, List<TableDefinition>>();

        public void AddTable(TableDefinition tableDefinition)
        {
            var schema = tableDefinition.Schema;

            if (_schemas.TryGetValue(schema, out var existingSchemaTables))
            {
                existingSchemaTables.Add(tableDefinition);
            }
            else
            {
                _schemas[schema] = new List<TableDefinition>() { tableDefinition };
            }
        }

        public TableDefinition GetTable(string schema, string tableName)
        {
            if (_schemas.TryGetValue(schema, out var existingSchemaTables))
            {
                return existingSchemaTables.FirstOrDefault(x => x.Name == tableName);
            }
            else
            {
                return null;
            }
        }

        public string[] GetSchemas()
        {
            return _schemas.Keys.ToArray();
        }

        public TableDefinition[] GetTablesOfSchema(string schema)
        {
            if (_schemas.TryGetValue(schema, out var existingSchemaTables))
            {
                return existingSchemaTables.ToArray();
            }
            else
            {
                return System.Array.Empty<TableDefinition>();
            }
        }
    }
}
