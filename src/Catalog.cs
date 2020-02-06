using System.Collections.Generic;
using System.Linq;

namespace Postgres.SchemaUpdater
{
    public class Catalog
    {
        private Dictionary<string, List<Table>> _schemas = new Dictionary<string, List<Table>>();

        public void AddTable(Table tableDefinition)
        {
            var schema = tableDefinition.Schema;

            if (_schemas.TryGetValue(schema, out var existingSchemaTables))
            {
                existingSchemaTables.Add(tableDefinition);
            }
            else
            {
                _schemas[schema] = new List<Table>() { tableDefinition };
            }
        }

        public Table GetTable(string schema, string tableName)
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

        public Table[] GetTablesOfSchema(string schema)
        {
            if (_schemas.TryGetValue(schema, out var existingSchemaTables))
            {
                return existingSchemaTables.ToArray();
            }
            else
            {
                return System.Array.Empty<Table>();
            }
        }
    }
}
