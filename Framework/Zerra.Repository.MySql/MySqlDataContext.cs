// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using MySql.Data.MySqlClient;
using Zerra.Logging;

namespace Zerra.Repository.MySql
{
    public abstract class MySqlDataContext : DataContext
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
                            var connectionForParsing = new MySqlConnectionStringBuilder(ConnectionString);
                        }
                        catch
                        {
                            _ = Log.InfoAsync($"{nameof(MySqlDataContext)} failed to parse {nameof(ConnectionString)}");
                        }
                        engine = new MySqlEngine(ConnectionString);
                    }
                }
            }
            return engine;
        }
    }
}
