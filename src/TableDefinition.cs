using System.Collections.Generic;

namespace Postgres.SchemaUpdater
{
    public class TableDefinition
    {
        public TableDefinition(string schema, string name, IEnumerable<TableColumn> columns)
        {
            Schema = schema;
            Name = name;
            Columns = columns;
        }

        public string Schema { get; }
        public string Name { get; }
        public IEnumerable<TableColumn> Columns { get; }
        public TableColumn PrimaryKey { get; set; }
    }
}
