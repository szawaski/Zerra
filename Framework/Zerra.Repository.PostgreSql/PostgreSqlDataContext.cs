// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Npgsql;
using Zerra.Logging;

namespace Zerra.Repository.PostgreSql
{
    /// <summary>
    /// Abstract base class for a PostgreSQL data context.
    /// </summary>
    public abstract class PostgreSqlDataContext : DataContext
    {
        /// <summary>
        /// Gets the PostgreSQL connection string used to connect to the database.
        /// </summary>
        public abstract string ConnectionString { get; }

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
                        try
                        {
                            var connectionForParsing = new NpgsqlConnectionStringBuilder(ConnectionString);
                        }
                        catch
                        {
                            Log.Info($"{nameof(PostgreSqlDataContext)} failed to parse {nameof(ConnectionString)}");
                        }
                        engine = new PostgreSqlEngine(ConnectionString);
                    }
                }
            }
            return engine;
        }
    }
}
