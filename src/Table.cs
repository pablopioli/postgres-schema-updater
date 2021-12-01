namespace Postgres.SchemaUpdater
{
    public class Table
    {
        public Table(string schema, string name, IList<Column>? columns = null)
        {
            Schema = schema;
            Name = name;

            if (columns != null)
            {
                foreach (var column in columns)
                {
                    Columns.Add(column);
                }
            }
        }

        public string Schema { get; } = string.Empty;
        public string Name { get; } = string.Empty;
        public IList<Column> Columns { get; } = new List<Column>();
        public IList<Index> Indexs { get; } = new List<Index>();
        public Column? PrimaryKey { get; set; }

        public Table AddColumn(Column column)
        {
            Columns.Add(column);
            return this;
        }

        public Table AddColumn(string name, string dataType)
        {
            Columns.Add(new Column(name, dataType));
            return this;
        }

        public Table AddIndex(Index index)
        {
            Indexs.Add(index);
            return this;
        }

        public Table AddIndex(string name, string expression)
        {
            Indexs.Add(new Index(name, expression));
            return this;
        }
    }
}
