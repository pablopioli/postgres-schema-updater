namespace Postgres.SchemaUpdater
{
    public class Column
    {
        public string Name { get; }
        public string DataType { get; }
        public bool Nullable { get; }

        public Column(string name, string dataType, bool nullable = false)
        {
            Name = name;
            DataType = dataType;
            Nullable = nullable;
        }
    }
}
