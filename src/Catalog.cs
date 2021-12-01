namespace Postgres.SchemaUpdater
{
    public class Catalog
    {
        private readonly Dictionary<string, List<Table>> _schemas = new();

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

        public Table? GetTable(string schema, string tableName)
        {
            if (_schemas.TryGetValue(schema, out var existingSchemaTables))
            {
                return existingSchemaTables.Find(x => x.Name == tableName);
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
                return Array.Empty<Table>();
            }
        }
    }
}
