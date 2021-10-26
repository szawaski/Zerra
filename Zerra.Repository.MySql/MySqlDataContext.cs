// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using MySql.Data.MySqlClient;
using Zerra.Logging;
using Zerra.Repository.Sql;

namespace Zerra.Repository.MySql
{
    public abstract class MySqlDataContext : DataContext<ISqlEngine>
    {
        public abstract string ConnectionString { get; }

        protected override sealed ISqlEngine GetEngine()
        {
            try
            {
                var connectionForParsing = new MySqlConnectionStringBuilder(ConnectionString);
                _ = Log.InfoAsync($"{nameof(MySqlDataContext)} connecting to {connectionForParsing.Database}");
            }
            catch
            {
                _ = Log.InfoAsync($"{nameof(MySqlDataContext)} failed to parse {ConnectionString}");
            }
            var engine = new MySqlEngine(ConnectionString);
            return engine;
        }
    }
}
