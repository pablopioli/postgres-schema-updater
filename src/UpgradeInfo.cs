namespace Postgres.SchemaUpdater
{
    internal class UpgradeInfo
    {
        private readonly ISet<string> _newSchemas = new HashSet<string>();
        private readonly ISet<Table> _newTables = new HashSet<Table>();
        private readonly ISet<Table> _modifiedTables = new HashSet<Table>();
        private readonly ISet<IndexInfo> _newIndexes = new HashSet<IndexInfo>();

        public ISet<string> NewSchemas()
        {
            return _newSchemas;
        }

        public ISet<Table> NewTables()
        {
            return _newTables;
        }

        public ISet<Table> ModifiedTables()
        {
            return _modifiedTables;
        }

        public ISet<IndexInfo> NewIndexes()
        {
            return _newIndexes;
        }

        public void AddNewSchema(string schema)
        {
            _newSchemas.Add(schema);
        }

        public void AddNewTable(Table table)
        {
            _newTables.Add(table);
        }

        public void AddModifiedTable(Table table)
        {
            _modifiedTables.Add(table);
        }

        public void AddNewIndex(IndexInfo index)
        {
            _newIndexes.Add(index);
        }

        internal class IndexInfo
        {
            public Table? Table { get; set; }
            public Index? Index { get; set; }
        }
    }
}
