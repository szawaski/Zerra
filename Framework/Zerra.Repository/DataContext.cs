// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Diagnostics.CodeAnalysis;
using Zerra.Collections;

namespace Zerra.Repository
{
    /// <summary>
    /// Base class for defining a data context that provides access to a data store engine.
    /// </summary>
    public abstract class DataContext
    {
        //a context type is one configuration, instances are created per provider so the data source is validated once per type
        private static readonly ConcurrentFactoryDictionary<Type, bool> validatedByType = new();

        /// <summary>
        /// Attempts to retrieve the validated data store engine.
        /// The data source is validated once per context type, every instance of the type shares the result.
        /// </summary>
        /// <param name="engine">When this method returns <see langword="true"/>, contains the validated <see cref="IDataStoreEngine"/>; otherwise, <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if the engine was retrieved and validated successfully; otherwise, <see langword="false"/>.</returns>
        public bool TryGetEngine(
#if !NETSTANDARD2_0
            [MaybeNullWhen(false)]
#endif
        out IDataStoreEngine engine)
        {
            engine = GetEngine();
            if (engine is null)
                return false;

            if (!validatedByType.GetOrAdd(GetType(), engine, static (x) => x.ValidateDataSource()))
            {
                engine = null;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Returns the <see cref="IDataStoreEngine"/> for this context, or <see langword="null"/> if unavailable.
        /// </summary>
        /// <returns>The <see cref="IDataStoreEngine"/> instance, or <see langword="null"/>.</returns>
        protected abstract IDataStoreEngine? GetEngine();
    }
}
