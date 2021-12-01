using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Postgres.SchemaUpdater
{
    public static class DdlTools
    {
        public static string ScriptCreateSchema(string schema)
        {
            return "Create Schema If Not Exists " + $"\"{schema.Replace("\"", "")}\"";
        }

        public static string ScriptDropTable(Table tableDefinition)
        {
            return "Drop Table If Exists " + $"\"{tableDefinition.Name.Replace("\"", "")}\"";
        }

        public static ICollection<string> ScriptCreateTable(Table tableDefinition, bool includeDropCommand)
        {
            var scripts = new List<string>();

            if (includeDropCommand)
            {
                scripts.Add(ScriptDropTable(tableDefinition));
            }

            var command = new StringBuilder();
            command.Append("Create Table ").Append(tableDefinition.Schema).Append('.').Append(tableDefinition.Name).Append(" (");

            Column? primaryKey = null;
            foreach (var column in tableDefinition.Columns)
            {
                command.Append(BuildColumn(column)).Append(", ");

                if (column == tableDefinition.PrimaryKey)
                {
                    primaryKey = column;
                }
            }
            command.Remove(command.ToString().Length - 2, 2);

            if (primaryKey != null)
            {
                command.Append(", CONSTRAINT PK_").Append(tableDefinition.Name).Append(" PRIMARY KEY (").Append(primaryKey.Name).Append(')');
            }

            command.Append(')');

            scripts.Add(command.ToString());

            foreach (var index in tableDefinition.Indexs)
            {
                scripts.Add(
                    $"CREATE INDEX {index.Name} on {tableDefinition.Schema}.{tableDefinition.Name} ({index.Expression})");
            }

            return scripts;
        }

        private static string BuildColumn(Column column)
        {
            return column.Name.ToLowerInvariant() + " " + column.DataType + (column.Nullable ? "" : " NOT NULL");
        }

        public static ICollection<string> ScriptCreateCatalog(Catalog catalog)
        {
            var scripts = new List<string>();

            foreach (var schema in catalog.GetSchemas())
            {
                scripts.Add(ScriptCreateSchema(schema));

                foreach (var table in catalog.GetTablesOfSchema(schema))
                {
                    scripts.AddRange(ScriptCreateTable(table, true));
                }
            }

            return scripts;
        }

        public static bool DatabaseExists(ServerSettings serverSettings)
        {
            var genericSettings = new ServerSettings
            {
                Server = serverSettings.Server,
                Username = serverSettings.Username,
                Password = serverSettings.Password,
                Database = "postgres"
            };

            using var connection = new NpgsqlConnection(genericSettings.GetConnectionString());
            connection.Open();

            var databaseExists = false;
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    "select datname from pg_catalog.pg_database where datname=@database";

                command.Parameters.Add(new Npgsql.NpgsqlParameter("database", serverSettings.Database));

                var res = command.ExecuteScalar();
                if (res != null)
                {
                    databaseExists = true;
                }
            }

            return databaseExists;
        }

        public static void CreateDatabase(ServerSettings serverSettings)
        {
            var genericSettings = new ServerSettings
            {
                Server = serverSettings.Server,
                Username = serverSettings.Username,
                Password = serverSettings.Password,
                Database = "postgres"
            };

            using var connection = new NpgsqlConnection(genericSettings.GetConnectionString());
            connection.Open();

            var databaseExists = false;
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    "select datname from pg_catalog.pg_database where datname=@database";

                command.Parameters.Add(new NpgsqlParameter("database", serverSettings.Database));

                var res = command.ExecuteScalar();
                if (res != null)
                {
                    databaseExists = true;
                }
            }

            if (!databaseExists)
            {
                using var command = connection.CreateCommand();
                // Parameters don't work here
                command.CommandText = "Create Database " + serverSettings.Database;
                command.ExecuteNonQuery();
            }
        }

        public static Catalog QuerySchema(ServerSettings serverSettings)
        {
            var catalog = new Catalog();

            using (var connection = new NpgsqlConnection(serverSettings.GetConnectionString()))
            {
                connection.Open();

                IList<SchemaInfo> schemaInfo;
                using (var command = connection.CreateCommand())
                {
                    command.CommandText =
@"SELECT t.table_schema as schemaname, t.table_name as tablename, c.column_name as columname
FROM information_schema.tables t
inner join Information_Schema.Columns c on t.table_name = c.table_name
where t.table_schema <> 'information_schema' and t.table_schema <> 'pg_catalog'";

                    using var reader = command.ExecuteReader();
                    schemaInfo = reader.ToList(x =>
                    new SchemaInfo(x.GetString(0), x.GetString(1), x.GetString(2)));
                }

                IList<SchemaIndex> indexes;
                using (var command = connection.CreateCommand())
                {
                    command.CommandText =
                        "SELECT schemaname as schema, relname as table, indexrelname as iname FROM pg_stat_all_indexes";

                    using var reader = command.ExecuteReader();
                    indexes = reader.ToList(x =>
                    new SchemaIndex { Schema = x.GetString(0), Table = x.GetString(1), Name = x.GetString(2) });
                }

                var groupedSchemas = from x in schemaInfo
                                     group x by x.Schema
                                         into tables
                                     select new
                                     {
                                         Schema = tables.Key,
                                         Tables = from y in tables
                                                  group y by y.Table
                                                          into fields
                                                  select new { Table = fields.Key, Fields = fields }
                                     };

                foreach (var schemaItem in groupedSchemas)
                {
                    foreach (var tableItem in schemaItem.Tables)
                    {
                        var columns = new List<Column>();
                        foreach (var field in tableItem.Fields)
                        {
                            columns.Add(new Column(field.Field, "unknown"));
                        }

                        var table = new Table(schemaItem.Schema, tableItem.Table, columns);

                        foreach (var index in indexes.Where(x => x.Schema == table.Schema && x.Table == tableItem.Table))
                        {
                            table.AddIndex(index.Name, string.Empty);
                        }

                        catalog.AddTable(table);
                    }
                }

                catalog = ReadPrimaryKeys(catalog, connection);
            }

            return catalog;
        }

        private static Catalog ReadPrimaryKeys(Catalog catalog, NpgsqlConnection connection)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
@"SELECT KU.TABLE_SCHEMA as Scheme, KU.table_name as TableName,column_name as PK
FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS AS TC
INNER JOIN
INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS KU
ON TC.CONSTRAINT_TYPE = 'PRIMARY KEY' AND
TC.CONSTRAINT_NAME = KU.CONSTRAINT_NAME
ORDER BY KU.TABLE_NAME, KU.ORDINAL_POSITION";

                using var reader = command.ExecuteReader();
                var pkData = reader.ToList(x => new PrimaryKeyData
                {
                    Schema = x.GetString(0),
                    TableName = x.GetString(1),
                    PrimaryKey = x.GetString(2)
                });

                foreach (var primaryKey in pkData)
                {
                    var table = catalog.GetTable(primaryKey.Schema, primaryKey.TableName);
                    if (table != null)
                    {
                        var pkColumn = table.Columns.FirstOrDefault(x => x.Name == primaryKey.PrimaryKey);
                        if (pkColumn != null)
                        {
                            table.PrimaryKey = pkColumn;
                        }
                    }
                }
            }

            return catalog;
        }

        private class PrimaryKeyData
        {
            internal string Schema = string.Empty;
            internal string TableName = string.Empty;
            internal string PrimaryKey = string.Empty;
        }

        public static ICollection<string> GenerateUpgradeScripts(Catalog catalog, ServerSettings serverSettings)
        {
            var currentSchema = QuerySchema(serverSettings);

            var diff = GetSchemaDifferences(currentSchema, catalog);

            var upgradeScripts = new List<string>();

            foreach (var schema in diff.NewSchemas())
            {
                upgradeScripts.Add(ScriptCreateSchema(schema));
            }

            foreach (var newTable in diff.NewTables())
            {
                upgradeScripts.AddRange(ScriptCreateTable(newTable, false));
            }

            foreach (var modifiedTable in diff.ModifiedTables())
            {
                upgradeScripts.AddRange(ScriptNewColumns(modifiedTable));
            }

            foreach (var index in diff.NewIndexes())
            {
                if (index.Index != null && index.Table != null)
                {
                    upgradeScripts.Add($"CREATE INDEX {index.Index.Name} on {index.Table.Schema}.{index.Table.Name} ({index.Index.Expression})");
                }
            }

            return upgradeScripts;
        }

        internal static UpgradeInfo GetSchemaDifferences(Catalog currentCatalog, Catalog desiredCatalog)
        {
            var upgradeInfo = new UpgradeInfo();

            foreach (var schema in desiredCatalog.GetSchemas().Except(currentCatalog.GetSchemas()))
            {
                upgradeInfo.AddNewSchema(schema);
            }

            foreach (var desiredSchema in desiredCatalog.GetSchemas())
            {
                var desiredTables = desiredCatalog.GetTablesOfSchema(desiredSchema);
                var currentTables = currentCatalog.GetTablesOfSchema(desiredSchema);

                foreach (var desiredTable in desiredTables)
                {
                    var currentTable = Array.Find(currentTables, x => x.Schema == desiredTable.Schema && x.Name == desiredTable.Name);

                    if (currentTable == null)
                    {
                        upgradeInfo.AddNewTable(desiredTable);
                    }
                    else
                    {
                        var columns = new List<Column>();
                        columns.AddRange(desiredTable.Columns.Except(currentTable.Columns, new ColumnComparer()));
                        if (columns.Count > 0)
                        {
                            var modifiedTable = new Table(desiredTable.Schema, desiredTable.Name, columns);
                            upgradeInfo.AddModifiedTable(modifiedTable);
                        }

                        foreach (var index in desiredTable.Indexs.Except(currentTable.Indexs, new IndexComparer()))
                        {
                            upgradeInfo.AddNewIndex(new UpgradeInfo.IndexInfo { Table = currentTable, Index = index });
                        }
                    }
                }
            }

            return upgradeInfo;
        }

        internal class ColumnComparer : IEqualityComparer<Column>
        {
            public bool Equals(Column? x, Column? y)
            {
                if (x == null || y == null)
                {
                    return false;
                }

                return x.Name == y.Name;
            }

            public int GetHashCode(Column obj)
            {
                if (obj == null)
                {
                    return 0;
                }

                return obj.Name.GetHashCode();
            }
        }

        internal class IndexComparer : IEqualityComparer<Index>
        {
            public bool Equals(Index? x, Index? y)
            {
                if (x == null || y == null)
                {
                    return false;
                }

                return x.Name == y.Name;
            }

            public int GetHashCode(Index obj)
            {
                if (obj == null)
                {
                    return 0;
                }

                return obj.Name.GetHashCode();
            }
        }

        private static ICollection<string> ScriptNewColumns(Table tableDefinition)
        {
            var result = new List<string>();

            var command = new StringBuilder();
            var firstPass = true;

            foreach (var column in tableDefinition.Columns)
            {
                if (firstPass)
                {
                    command.Append("Alter Table ").Append(tableDefinition.Schema).Append('.').Append(tableDefinition.Name).Append(" Add ");
                    firstPass = false;
                }
                else
                {
                    command.Append(", ");
                }

                command.Append(BuildColumn(column));
            }

            result.Add(command.ToString());

            return result;
        }

        private class SchemaIndex
        {
            public string Schema = string.Empty;
            public string Table = string.Empty;
            public string Name = string.Empty;
        }
    }
}
