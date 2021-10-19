// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Npgsql;
using Zerra.Logging;

namespace Zerra.Repository.Sql.PostgreSql
{
    public abstract class PostgreSqlDataContext : SqlDataContext
    {
        public abstract string ConnectionString { get; }

        private static ISqlEngine provider;
        private static readonly object providerLock = new object();

        public override sealed ISqlEngine GetEngine()
        {
            if (provider == null)
            {
                lock (providerLock)
                {
                    if (provider == null)
                    {
                        var connectionForParsing = new NpgsqlConnectionStringBuilder(ConnectionString);
                        _ = Log.InfoAsync($"{nameof(PostgreSqlDataContext)} connecting to {connectionForParsing.Database}");
                        provider = new PostgreSqlEngine(ConnectionString);
                    }
                }
            }
            return provider;
        }
    }
}
