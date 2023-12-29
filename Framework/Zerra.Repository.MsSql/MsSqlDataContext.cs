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

        private readonly object locker = new();
        private IDataStoreEngine? engine = null;
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
                            var connectionForParsing = new SqlConnectionStringBuilder(ConnectionString);
                        }
                        catch
                        {
                            _ = Log.InfoAsync($"{nameof(MsSqlDataContext)} failed to parse {nameof(ConnectionString)}");
                        }
                        engine = new MsSqlEngine(ConnectionString);
                    }
                }
            }
            return engine;
        }
    }
}
