// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Reflection
{
    /// <summary>
    /// Provides extension methods for type analysis using the Zerra framework.
    /// Enables convenient access to type detail information via extension method syntax.
    /// </summary>
    public static class TypeAnalyzerExtensions
    {
        /// <summary>
        /// Gets detailed type information for the specified type using extension method syntax.
        /// This is a convenience wrapper around <see cref="TypeAnalyzer.GetTypeDetail(Type)"/>.
        /// </summary>
        /// <param name="it">The type to analyze.</param>
        /// <returns>Cached or newly generated type detail information for the specified type.</returns>
        /// <exception cref="NotSupportedException">Thrown if dynamic code generation is required but not supported.</exception>
        public static TypeDetail GetTypeDetail(this Type it) => TypeAnalyzer.GetTypeDetail(it);
    }
}
