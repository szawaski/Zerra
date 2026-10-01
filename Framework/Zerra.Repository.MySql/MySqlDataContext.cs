// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using MySql.Data.MySqlClient;
using Zerra.Logging;

namespace Zerra.Repository.MySql
{
    /// <summary>
    /// Creates MySQL engines and checks whether a MySQL server can be reached, such as at startup to choose between MySQL and another data store.
    /// </summary>
    public static class MySqlDataContext
    {
        /// <summary>
        /// Creates an engine for the MySQL database. It doesn't connect until it's used.
        /// </summary>
        /// <param name="connectionString">The MySQL connection string.</param>
        /// <returns>The engine to give the store providers and <see cref="CodeFirstGeneration"/>.</returns>
        public static MySqlEngine GetEngine(string connectionString)
        {
            if (String.IsNullOrWhiteSpace(connectionString)) throw new ArgumentNullException(nameof(connectionString));
            return new MySqlEngine(connectionString);
        }

        /// <summary>
        /// Tests the connection by asking the server for its version on the sys database, so the database itself doesn't have to exist yet.
        /// </summary>
        /// <param name="connectionString">The MySQL connection string, its Connect Timeout sets how long to wait.</param>
        /// <param name="log">Optional logger, told why the connection failed.</param>
        /// <returns>True if the server answered; otherwise false.</returns>
        public static bool TestConnection(string connectionString, ILogger? log = null)
        {
            if (String.IsNullOrWhiteSpace(connectionString)) throw new ArgumentNullException(nameof(connectionString));

            const string sql = "SELECT version()";

            try
            {
                var builder = new MySqlConnectionStringBuilder(connectionString);
                builder.Database = "sys";
                var connectionStringForMaster = builder.ToString();

                using (var connection = new MySqlConnection(connectionStringForMaster))
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandTimeout = 0;
                        command.CommandText = sql;
                        var version = (string)command.ExecuteScalar();
                        if (version.Length > 0 && Char.IsNumber(version[0]))
                            return true;

                        log?.Warn($"{nameof(MySqlDataContext)} could not connect: Invalid version {version}");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                log?.Warn($"{nameof(MySqlDataContext)} could not connect: {ex.Message}");
                return false;
            }
        }
    }
}
