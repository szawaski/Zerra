// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using MySqlConnector;
using Zerra.Logging;

namespace Zerra.Repository.MariaDb
{
    /// <summary>
    /// Abstract base class for a MariaDb data context.
    /// </summary>
    public abstract class MariaDbDataContext : DataContext
    {
        /// <summary>
        /// Gets the MariaDb connection string used to connect to the database.
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
                            Log.Info($"{nameof(MariaDbDataContext)} failed to parse connection string");
                        }
                        engine = new MariaDbEngine(connectionString);
                    }
                }
            }
            return engine;
        }
    }
}
