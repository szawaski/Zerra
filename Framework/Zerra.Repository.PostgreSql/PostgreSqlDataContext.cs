// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Npgsql;
using Zerra.Logging;

namespace Zerra.Repository.PostgreSql
{
    public abstract class PostgreSqlDataContext : DataContext<ITransactStoreEngine>
    {
        public abstract string ConnectionString { get; }

        protected override sealed ITransactStoreEngine GetEngine()
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
            var engine = new PostgreSqlEngine(ConnectionString);
            return engine;
        }
    }
}
