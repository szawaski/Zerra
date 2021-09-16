// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository.Sql.MsSql
{
    public abstract class MsSqlDataContext : SqlDataContext
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
                        provider = new MsSqlEngine(ConnectionString);
                    }
                }
            }
            return provider;
        }
    }
}
