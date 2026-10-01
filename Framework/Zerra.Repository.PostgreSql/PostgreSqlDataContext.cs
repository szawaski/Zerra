// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Npgsql;
using Zerra.Logging;

namespace Zerra.Repository.PostgreSql
{
    /// <summary>
    /// Creates PostgreSQL engines and checks whether a PostgreSQL server can be reached, such as at startup to choose between PostgreSQL and another data store.
    /// </summary>
    public static class PostgreSqlDataContext
    {
        /// <summary>
        /// Creates an engine for the PostgreSQL database. It doesn't connect until it's used.
        /// </summary>
        /// <param name="connectionString">The PostgreSQL connection string.</param>
        /// <returns>The engine to give the store providers and <see cref="CodeFirstGeneration"/>.</returns>
        public static PostgreSqlEngine GetEngine(string connectionString)
        {
            if (String.IsNullOrWhiteSpace(connectionString)) throw new ArgumentNullException(nameof(connectionString));
            return new PostgreSqlEngine(connectionString);
        }

        /// <summary>
        /// Tests the connection by asking the server for its version on the postgres database, so the database itself doesn't have to exist yet.
        /// </summary>
        /// <param name="connectionString">The PostgreSQL connection string, its Timeout sets how long to wait.</param>
        /// <param name="log">Optional logger, told why the connection failed.</param>
        /// <returns>True if the server answered; otherwise false.</returns>
        public static bool TestConnection(string connectionString, ILogger? log = null)
        {
            if (String.IsNullOrWhiteSpace(connectionString)) throw new ArgumentNullException(nameof(connectionString));

            const string sql = "SELECT version()";

            try
            {
                var builder = new NpgsqlConnectionStringBuilder(connectionString);
                builder.Database = "postgres";
                var connectionStringForMaster = builder.ToString();

                using (var connection = new NpgsqlConnection(connectionStringForMaster))
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandTimeout = 0;
                        command.CommandText = sql;
                        var version = (string)command.ExecuteScalar()!;
                        if (version.Contains("PostgreSQL"))
                            return true;

                        log?.Warn($"{nameof(PostgreSqlDataContext)} could not connect: Invalid version {version}");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                log?.Warn($"{nameof(PostgreSqlDataContext)} could not connect: {ex.Message}");
                return false;
            }
        }
    }
}
