// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository.KurrentDB
{
    /// <summary>
    /// Abstract base class for KurrentDB data contexts that provides connection management and engine initialization.
    /// </summary>
    public abstract class KurrentDBDataContext : DataContext
    {
        /// <summary>
        /// Gets the connection string used to connect to the KurrentDB instance.
        /// </summary>
        public abstract string ConnectionString { get; }
        /// <summary>
        /// Gets a value indicating whether to use an insecure connection (without TLS/SSL).
        /// </summary>
        public abstract bool Insecure { get; }

        private readonly object locker = new();
        private IDataStoreEngine? engine = null;
        /// <inheritdoc/>
        protected override IDataStoreEngine GetEngine()
        {
            if (engine is null)
            {
                lock (locker)
                {
                    if (engine is null)
                    {
                        engine = new KurrentDBEngine(ConnectionString, Insecure);
                        return engine;
                    }
                }
            }
            return engine;
        }
    }
}
