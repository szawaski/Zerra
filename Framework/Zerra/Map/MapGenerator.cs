// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Map.Converters;
using Zerra.Reflection;

namespace Zerra.Map
{
    /// <summary>
    /// Provides static methods to generate mapping functions that transform objects from a source type to a target type.
    /// </summary>
    public static class MapGenerator
    {
        /// <summary>
        /// Generates a mapping function that transforms instances of <typeparamref name="TSource"/> to <typeparamref name="TTarget"/>.
        /// </summary>
        /// <typeparam name="TSource">The source type to map from. Must be a non-nullable reference type.</typeparam>
        /// <typeparam name="TTarget">The target type to map to. Must be a non-nullable reference type.</typeparam>
        /// <returns>A mapping function that accepts a source object, optional target object, and optional graph, and returns the mapped target object.</returns>
        public static Func<TSource, TTarget?, Graph?, TTarget?> Generate<TSource, TTarget>()
            where TSource : notnull
            where TTarget : notnull
            => Map<TSource, TTarget>;

        private static TTarget? Map<TSource, TTarget>(TSource source, TTarget? target, Graph? graph)
            where TSource : notnull
            where TTarget : notnull
        {
            var sourceTypeDetail = TypeAnalyzer<TSource>.GetTypeDetail();
            var targetTypeDetail = TypeAnalyzer<TTarget>.GetTypeDetail();
            var converter = (MapConverter<TSource, TTarget>)MapConverterFactory.GetRoot(sourceTypeDetail, targetTypeDetail);
            var resultTarget = converter.Map(source, target, graph);
            return resultTarget;
        }

        /// <summary>
        /// Generates a mapping function that transforms instances of any type to <typeparamref name="TTarget"/>.
        /// </summary>
        /// <typeparam name="TTarget">The target type to map to. Must be a non-nullable reference type.</typeparam>
        /// <returns>A mapping function that accepts a source object of any type, optional target object, and optional graph, and returns the mapped target object.</returns>
        public static Func<object, TTarget?, Graph?, TTarget?> Generate<TTarget>()
            where TTarget : notnull
            => Map<TTarget>;

        private static TTarget? Map<TTarget>(object source, TTarget? target, Graph? graph)
            where TTarget : notnull
        {
            var sourceTypeDetail = source.GetType().GetTypeDetail();
            var targetTypeDetail = TypeAnalyzer<TTarget>.GetTypeDetail();
            var converter = MapConverterFactory.GetRoot(sourceTypeDetail, targetTypeDetail);
            var resultTarget = (TTarget?)converter.Map(source, target, graph);
            return resultTarget;
        }
    }
}
