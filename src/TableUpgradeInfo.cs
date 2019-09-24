using System.Collections.Generic;

namespace Postgres.SchemaUpdater
{
    internal class TableUpgradeInfo
    {
        private readonly ISet<string> _newSchemas = new HashSet<string>();
        private readonly ISet<TableDefinition> _newTables = new HashSet<TableDefinition>();
        private readonly ISet<TableDefinition> _modifiedTables = new HashSet<TableDefinition>();

        public ISet<string> NewSchemas()
        {
            return _newSchemas;
        }

        public ISet<TableDefinition> NewTables()
        {
            return _newTables;
        }

        public ISet<TableDefinition> ModifiedTables()
        {
            return _modifiedTables;
        }

        public void AddNewSchema(string schema)
        {
            _newSchemas.Add(schema);
        }

        public void AddNewTable(TableDefinition table)
        {
            _newTables.Add(table);
        }

        public void AddModifiedTable(TableDefinition table)
        {
            _modifiedTables.Add(table);
        }
    }
}
