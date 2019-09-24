using Npgsql;
using Postgres.SchemaUpdater;
using System;
using System.Collections.Generic;

namespace samples
{
    static class Program
    {
        static void Main(string[] args)
        {
            var set = new ServerSettings
            {
                Server = "localhost",
                Username = "fill this",
                Password = "fill this",
                Database = "fill this"
            };

            DdlTools.CreateDatabase(set);

            var catalog = new TableCatalog();

            var columns = new List<TableColumn>()
            {
                new TableColumn("id", "uuid", nullable: false),
                new TableColumn("name", "varchar", nullable: false)
            };
            var table = new TableDefinition("crm", "persons", columns);
            catalog.AddTable(table);

            var scripts = DdlTools.GenerateUpgradeScripts(catalog, set);

            using (var connection = new NpgsqlConnection(set.GetConnectionString()))
            {
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
}
