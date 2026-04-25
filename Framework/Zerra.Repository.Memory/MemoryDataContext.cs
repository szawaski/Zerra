//// Copyright © KaKush LLC
//// Written By Steven Zawaski
//// Licensed to you under the MIT license

//using Microsoft.Data.SqlClient;
//using Zerra.Logging;

//namespace Zerra.Repository.Memory
//{
//    /// <summary>
//    /// Abstract base class for a Memory data context.
//    /// </summary>
//    public abstract class MemoryDataContext : DataContext
//    {
//        private readonly Lock locker = new();
//        private IDataStoreEngine? engine = null;
//        /// <inheritdoc/>
//        protected override sealed IDataStoreEngine GetEngine()
//        {
//            if (engine is null)
//            {
//                lock (locker)
//                {
//                    engine ??= new MemoryEngine();
//                }
//            }
//            return engine;
//        }
//    }
//}
