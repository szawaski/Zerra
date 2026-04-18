// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Diagnostics.CodeAnalysis;

namespace Zerra.Repository
{
    public abstract class DataContext
    {
        private static bool isValid = false;
        private static bool validated = false;
        private static readonly object validatedLock = new();

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

        protected abstract IDataStoreEngine? GetEngine();
    }
}
