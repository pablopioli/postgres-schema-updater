using Npgsql;
using Postgres.SchemaUpdater;

namespace samples
{
    public static class Program
    {
        private static void Main()
        {
            var set = new ServerSettings
            {
                Server = "localhost",
                Username = "fill this",
                Password = "fill this",
                Database = "fill this"
            };

            // You can create the database if you have sufficient rights
            // DdlTools.CreateDatabase(set);

            var catalog = new Catalog();

            var columns = new List<Column>()
            {
                new Column("id", "uuid", nullable: false),
                new Column("name", "varchar", nullable: false)
            };

            var table = new Table("crm", "persons", columns).AddIndex("name", "(name)");

            catalog.AddTable(table);

            var scripts = DdlTools.GenerateUpgradeScripts(catalog, set);

            using var connection = new NpgsqlConnection(set.GetConnectionString());
            connection.Open();

            foreach (var script in scripts)
            {
                Console.WriteLine(script);

                var command = connection.CreateCommand();
                command.CommandText = script;
                command.ExecuteNonQuery();
            }
        }
    }
}
