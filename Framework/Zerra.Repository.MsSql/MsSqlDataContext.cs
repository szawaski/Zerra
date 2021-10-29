// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Data.SqlClient;
using Zerra.Logging;

namespace Zerra.Repository.MsSql
{
    public abstract class MsSqlDataContext : DataContext
    {
        public abstract string ConnectionString { get; }

        protected override sealed IDataStoreEngine GetEngine()
        {
            try
            {
                var connectionForParsing = new SqlConnectionStringBuilder(ConnectionString);
                _ = Log.InfoAsync($"{nameof(MsSqlDataContext)} connecting to {connectionForParsing.DataSource}");
            }
            catch
            {
                _ = Log.InfoAsync($"{nameof(MsSqlDataContext)} failed to parse {ConnectionString}");
            }
            var engine = new MsSqlEngine(ConnectionString);
            return engine;
        }
    }
}
