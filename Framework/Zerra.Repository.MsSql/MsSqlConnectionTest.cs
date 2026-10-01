// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.Data.SqlClient;
using Zerra.Logging;

namespace Zerra.Repository.MsSql
{
    /// <summary>
    /// Checks whether a SQL Server can be reached, such as at startup to choose between SQL Server and another data store.
    /// </summary>
    public static class MsSqlConnectionTest
    {
        /// <summary>
        /// Tests the connection by asking the server for its version on the master database, so the database itself doesn't have to exist yet.
        /// </summary>
        /// <param name="connectionString">The SQL Server connection string, its Connect Timeout sets how long to wait.</param>
        /// <param name="log">Optional logger, told why the connection failed.</param>
        /// <returns>True if the server answered; otherwise false.</returns>
        public static bool Test(string connectionString, ILogger? log = null)
        {
            if (String.IsNullOrWhiteSpace(connectionString)) throw new ArgumentNullException(nameof(connectionString));

            const string sql = "SELECT @@version";

            try
            {
                var builder = new SqlConnectionStringBuilder(connectionString);
                builder.InitialCatalog = "master";
                var connectionStringForMaster = builder.ToString();

                using (var connection = new SqlConnection(connectionStringForMaster))
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandTimeout = 0;
                        command.CommandText = sql;
                        var version = (string)command.ExecuteScalar();
                        if (version.Contains("Microsoft SQL"))
                            return true;

                        log?.Warn($"{nameof(MsSqlConnectionTest)} could not connect: Invalid version {version}");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                log?.Warn($"{nameof(MsSqlConnectionTest)} could not connect: {ex.Message}");
                return false;
            }
        }
    }
}
