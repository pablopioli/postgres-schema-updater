﻿using System.Linq;
using System.Collections.Generic;
using Npgsql;
using System.Text;

namespace Postgres.SchemaUpdater
{
    public static class DdlTools
    {
        public static string ScriptCreateSchema(string schema)
        {
            return "Create Schema If Not Exists " + $"\"{schema.Replace("\"", "")}\"";
        }

        public static string ScriptDropTable(TableDefinition tableDefinition)
        {
            return "Drop Table If Exists " + $"\"{tableDefinition.Name.Replace("\"", "")}\"";
        }

        public static ICollection<string> ScriptCreateTable(TableDefinition tableDefinition, bool includeDropCommand)
        {
            var scripts = new List<string>();

            if (includeDropCommand)
            {
                scripts.Add(ScriptDropTable(tableDefinition));
            }

            var command = new StringBuilder();
            command.Append("Create Table " + $"{tableDefinition.Schema}.{tableDefinition.Name}" + " (");

            TableColumn primaryKey = null;
            foreach (var column in tableDefinition.Columns)
            {
                command.Append(BuildColumn(column) + ", ");

                if (column == tableDefinition.PrimaryKey)
                {
                    primaryKey = column;
                }
            }
            command.Remove(command.ToString().Length - 2, 2);

            if (primaryKey != null)
            {
                command.Append(", CONSTRAINT PK_" + tableDefinition.Name + " PRIMARY KEY (" + primaryKey.Name + ")");
            }

            command.Append(")");

            scripts.Add(command.ToString());

            return scripts;
        }

        private static string BuildColumn(TableColumn column)
        {
            return column.Name.ToLowerInvariant() + " " + column.DataType + (column.Nullable ? "" : " NOT NULL");
        }

        public static ICollection<string> ScriptCreateCatalog(TableCatalog catalog)
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

            using (var connection = new NpgsqlConnection(genericSettings.GetConnectionString()))
            {
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

            using (var connection = new NpgsqlConnection(genericSettings.GetConnectionString()))
            {
                connection.Open();

                var databaseExists = false;
                using (var command = connection.CreateCommand())
                {
                    command.CommandText =
                        "select datname from pg_catalog.pg_database where datname=@database";

                    command.Parameters.Add(new NpgsqlParameter("database", serverSettings.Database));

                    var res = command.ExecuteScalar();
                    if (res != null)
                        databaseExists = true;
                }

                if (!databaseExists)
                {
                    using (var command = connection.CreateCommand())
                    {
#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
                        // Parameters don't work here
                        command.CommandText = "Create Database " + serverSettings.Database;
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities
                        command.ExecuteNonQuery();
                    }
                }
            }
        }

        public static TableCatalog QuerySchema(ServerSettings serverSettings)
        {
            var catalog = new TableCatalog();

            using (var connection = new NpgsqlConnection(serverSettings.GetConnectionString()))
            {
                connection.Open();

                IList<SchemaInfo> schemaInfo;
                using (var command = connection.CreateCommand())
                {
                    var commandText =
                        @"SELECT t.table_schema as schemaname, t.table_name as tablename, c.column_name as columname
FROM information_schema.tables t
inner join Information_Schema.Columns c on t.table_name = c.table_name
where t.table_schema <> 'information_schema' and t.table_schema <> 'pg_catalog'";

                    command.CommandText = commandText;

                    using (var reader = command.ExecuteReader())
                    {
                        schemaInfo = reader.ToList(x =>
                        new SchemaInfo(x.GetString(0), x.GetString(1), x.GetString(2)));
                    }
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
                        var columns = new List<TableColumn>();
                        foreach (var field in tableItem.Fields)
                        {
                            columns.Add(new TableColumn(field.Field, "unknown"));
                        }

                        var table = new TableDefinition(schemaItem.Schema, tableItem.Table, columns);
                        catalog.AddTable(table);
                    }
                }

                catalog = ReadPrimaryKeys(catalog, connection);
            }

            return catalog;
        }

        private static TableCatalog ReadPrimaryKeys(TableCatalog catalog, NpgsqlConnection connection)
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

                using (var reader = command.ExecuteReader())
                {
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
            }

            return catalog;
        }

        private class PrimaryKeyData
        {
            internal string Schema;
            internal string TableName;
            internal string PrimaryKey;
        }

        public static ICollection<string> GenerateUpgradeScripts(TableCatalog catalog, ServerSettings serverSettings)
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

            return upgradeScripts;
        }

        internal static TableUpgradeInfo GetSchemaDifferences(TableCatalog currentCatalog, TableCatalog desiredCatalog)
        {
            var upgradeInfo = new TableUpgradeInfo();

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
                    var currentTable = currentTables.FirstOrDefault(x => x.Schema == desiredTable.Schema && x.Name == desiredTable.Name);

                    if (currentTable == null)
                    {
                        upgradeInfo.AddNewTable(desiredTable);
                    }
                    else
                    {
                        var columns = new List<TableColumn>();
                        columns.AddRange(desiredTable.Columns.Except(currentTable.Columns, new ColumnComparer()));
                        if (columns.Count > 0)
                        {
                            var modifiedTable = new TableDefinition(desiredTable.Schema, desiredTable.Name, columns);
                            upgradeInfo.AddModifiedTable(modifiedTable);
                        }
                    }
                }
            }

            return upgradeInfo;
        }

        internal class ColumnComparer : IEqualityComparer<TableColumn>
        {
            public bool Equals(TableColumn x, TableColumn y)
            {
                if (x == null || y == null)
                {
                    return false;
                }

                return x.Name == y.Name;
            }

            public int GetHashCode(TableColumn obj)
            {
                if (obj == null)
                {
                    return 0;
                }

                return obj.Name.GetHashCode();
            }
        }

        private static ICollection<string> ScriptNewColumns(TableDefinition tableDefinition)
        {
            var result = new List<string>();

            var command = new StringBuilder();
            var firstPass = true;

            foreach (var column in tableDefinition.Columns)
            {
                if (firstPass)
                {
                    command.Append($"Alter Table {tableDefinition.Schema}.{tableDefinition.Name} Add ");
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

    }
}
