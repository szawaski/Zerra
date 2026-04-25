// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license


// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Reflection
{
    /// <summary>
    /// Provides generic type analysis for a specific type <typeparamref name="T"/>.
    /// Caches detailed type information in a thread-safe manner for performance.
    /// This is a generic wrapper around the non-generic <see cref="TypeAnalyzer"/> that specializes access for a single type.
    /// </summary>
    /// <typeparam name="T">The type to provide analysis for.</typeparam>
    public static class TypeAnalyzer<T>
    {
        private static readonly Lock typeDetailLock = new Lock();
        private static TypeDetail<T>? typeDetail = null;
        
        /// <summary>
        /// Gets the cached detailed type information for <typeparamref name="T"/>.
        /// Thread-safe lazy initialization ensures the type detail is generated only once.
        /// </summary>
        /// <returns>The type detail information for <typeparamref name="T"/>.</returns>
        /// <exception cref="NotSupportedException">Thrown if dynamic code generation is required but not supported.</exception>
        public static TypeDetail<T> GetTypeDetail()
        {
            if (typeDetail is null)
            {
                lock (typeDetailLock)
                    typeDetail ??= (TypeDetail<T>)TypeAnalyzer.GetTypeDetail(typeof(T));
            }
            return typeDetail;
        }
    }
}
