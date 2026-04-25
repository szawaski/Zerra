// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using MySql.Data.MySqlClient;
using Zerra.Logging;

namespace Zerra.Repository.MySql
{
    /// <summary>
    /// Abstract base class for a MySQL data context.
    /// </summary>
    public abstract class MySqlDataContext : DataContext
    {
        /// <summary>
        /// Gets the MySQL connection string used to connect to the database.
        /// </summary>
        public abstract string GetConnectionString();

        private readonly Lock locker = new();
        private IDataStoreEngine? engine = null;
        /// <inheritdoc/>
        protected override sealed IDataStoreEngine GetEngine()
        {
            if (engine is null)
            {
                lock (locker)
                {
                    if (engine is null)
                    {
                        var connectionString = GetConnectionString();
                        try
                        {
                            var connectionForParsing = new MySqlConnectionStringBuilder(connectionString);
                        }
                        catch
                        {
                            Log.Info($"{nameof(MySqlDataContext)} failed to parse connection string");
                        }
                        engine = new MySqlEngine(connectionString);
                    }
                }
            }
            return engine;
        }
    }
}
