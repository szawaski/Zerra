// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Npgsql;
using Zerra.Logging;

namespace Zerra.Repository.PostgreSql
{
    public abstract class PostgreSqlDataContext : DataContext
    {
        public abstract string ConnectionString { get; }

        private readonly object locker = new();
        private IDataStoreEngine engine = null;
        protected override sealed IDataStoreEngine GetEngine()
        {
            if (engine == null)
            {
                lock (locker)
                {
                    if (engine == null)
                    {
                        try
                        {
                            var connectionForParsing = new NpgsqlConnectionStringBuilder(ConnectionString);
                            _ = Log.InfoAsync($"{nameof(PostgreSqlDataContext)} connecting to {connectionForParsing.Host},{connectionForParsing.Port}");
                        }
                        catch
                        {
                            _ = Log.InfoAsync($"{nameof(PostgreSqlDataContext)} failed to parse {ConnectionString}");
                        }
                        engine = new PostgreSqlEngine(ConnectionString);
                    }
                }
            }
            return engine;
        }
    }
}
