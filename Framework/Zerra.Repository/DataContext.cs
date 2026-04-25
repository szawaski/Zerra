// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Diagnostics.CodeAnalysis;

namespace Zerra.Repository
{
    /// <summary>
    /// Base class for defining a data context that provides access to a data store engine.
    /// </summary>
    public abstract class DataContext
    {
        private static bool isValid = false;
        private static bool validated = false;
        private static readonly object validatedLock = new();

        /// <summary>
        /// Attempts to retrieve the validated data store engine.
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

            lock (validatedLock)
            {
                if (!validated)
                {
                    validated = true;
                    isValid = engine.ValidateDataSource();
                }

                if (!isValid)
                {
                    engine = null;
                    return false;
                }
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
