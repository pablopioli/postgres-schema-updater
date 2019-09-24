namespace Postgres.SchemaUpdater
{
    public class SchemaInfo
    {
        public string Schema { get; }
        public string Table { get; }
        public string Field { get; }

        public SchemaInfo(string schema, string table, string field)
        {
            Schema = schema;
            Table = table;
            Field = field;
        }
    }
}
