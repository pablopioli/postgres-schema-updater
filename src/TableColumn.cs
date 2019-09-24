namespace Postgres.SchemaUpdater
{
    public class TableColumn
    {
        public string Name { get; }
        public string DataType { get; }
        public bool Nullable { get; }

        public TableColumn(string name, string dataType, bool nullable = false)
        {
            Name = name;
            DataType = dataType;
            Nullable = nullable;
        }
    }
}
