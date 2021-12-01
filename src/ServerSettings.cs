using System;
using System.Text;

namespace Postgres.SchemaUpdater
{
    public class ServerSettings
    {
        public string Server { get; set; }
        public int Port { get; set; }
        public string Database { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string CustomParameters { get; set; } = string.Empty;
        public string Name { get; set; }

        public ServerSettings()
        {
            Server = string.Empty;
            Port = 0;
            Database = string.Empty;
            Name = string.Empty;
            Username = string.Empty;
            Password = string.Empty;
            CustomParameters = string.Empty;
        }

        public string GetConnectionString()
        {
            var connString = new StringBuilder();
            connString.Append(CustomParameters);

            if (string.IsNullOrEmpty(Server))
            {
                throw new Exception("No server address provided");
            }
            else
            {
                connString.AppendFormat("Server={0};", Server);
            }

            var port = Port;
            if (port == 0)
            {
                port = 5432;
            }
            connString.AppendFormat("Port={0};", port.ToString());

            if (string.IsNullOrEmpty(Database))
            {
                throw new Exception("No database name provided");
            }
            else
            {
                connString.AppendFormat("Database={0};", Database);
            }

            if (string.IsNullOrEmpty(Username))
            {
                throw new Exception("No username provided");
            }
            else
            {
                connString.AppendFormat("User Id={0};", Username);
            }

            if (string.IsNullOrEmpty(Password))
            {
                throw new Exception("No password provided");
            }
            else
            {
                connString.AppendFormat("Password={0};", Password);
            }

            return connString.ToString();
        }
    }
}
