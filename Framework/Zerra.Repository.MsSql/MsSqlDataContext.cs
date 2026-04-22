// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.Data.SqlClient;
using Zerra.Logging;

namespace Zerra.Repository.MsSql
{
    /// <summary>
    /// Abstract base class for a Microsoft SQL Server data context.
    /// </summary>
    public abstract class MsSqlDataContext : DataContext
    {
        /// <summary>
        /// Gets the SQL Server connection string used to connect to the database.
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
                            var connectionForParsing = new SqlConnectionStringBuilder(ConnectionString);
                        }
                        catch
                        {
                            Log.Info($"{nameof(MsSqlDataContext)} failed to parse {nameof(ConnectionString)}");
                        }
                        engine = new MsSqlEngine(ConnectionString);
                    }
                }
            }
            return engine;
        }
    }
}
