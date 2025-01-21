// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.Collections;
using Zerra.Reflection;

namespace Zerra.Map
{
    /// <summary>
    /// Methods for mapping properties of a source object to a target object.
    /// </summary>
    public static class Mapper
    {
        internal static bool DebugMode { get; set; } = Config.IsDebugBuild;

        private static readonly Type mapType = typeof(MapGenerator<,>);
        private static readonly ConcurrentFactoryDictionary<TypeKey, Func<object, Graph?, object>> copyFuncs = new();
        private static readonly ConcurrentFactoryDictionary<TypeKey, Func<object, object, Graph?, object>> copyToFuncs = new();

        /// <summary>
        /// Creates a new object of the target type and populates the members from the source object.
        /// Matching names are automatically mapped and will attempt to convert types.
        /// Customizations are made by implementing <see cref="IMapDefinition{TSource, TTarget}"/>
        /// </summary>
        /// <typeparam name="TSource">The source type that will be mapped.</typeparam>
        /// <typeparam name="TTarget">The target type that will be mapped.</typeparam>
        /// <param name="source">The source object for the mapping.</param>
        /// <param name="graph">A graph to filter which properties to map.</param>
        /// <returns>The new object of the target type with members populated from the source.</returns>
        public static TTarget Map<TSource, TTarget>(this TSource source, Graph? graph = null)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            var map = MapGenerator<TSource, TTarget>.GetMap();
            return map.Copy(source, graph);
        }

        /// <summary>
        /// Populates the members of the target object from the source object.
        /// Matching names are automatically mapped and will attempt to convert types.
        /// Customizations are made by implementing <see cref="IMapDefinition{TSource, TTarget}"/>
        /// </summary>
        /// <typeparam name="TSource">The source type that will be mapped.</typeparam>
        /// <typeparam name="TTarget">The target type that will be mapped.</typeparam>
        /// <param name="source">The source object for the mapping.</param>
        /// <param name="graph">A graph to filter which properties to map.</param>
        public static void MapTo<TSource, TTarget>(this TSource source, TTarget target, Graph? graph = null)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (target is null)
                throw new ArgumentNullException(nameof(target));

            var map = MapGenerator<TSource, TTarget>.GetMap();
            _ = map.CopyTo(source, target, graph);
        }

        /// <summary>
        /// Creates a new object of the target type and populates the members from the source object.
        /// Matching names are automatically mapped and will attempt to convert types.
        /// Customizations are made by implementing <see cref="IMapDefinition{TSource, TTarget}"/>
        /// </summary>
        /// <typeparam name="TTarget">The target type that will be mapped.</typeparam>
        /// <param name="source">The source object for the mapping.</param>
        /// <param name="graph">A graph to filter which properties to map.</param>
        /// <returns>The new object of the target type with members populated from the source.</returns>
        public static TTarget Map<TTarget>(this object source, Graph? graph = null)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            var sourceType = source.GetType();
            var targetType = typeof(TTarget);

            var key = new TypeKey(sourceType, targetType);
            var copyFunc = copyFuncs.GetOrAdd(key, sourceType, targetType, static (sourceType, targetType) =>
            {
                var genericMapType = TypeAnalyzer.GetGenericTypeDetail(mapType, sourceType, targetType);
                var map = genericMapType.GetMethodBoxed("GetMap").CallerBoxed(null, null);
                return (s, g) => { return genericMapType.GetMethodBoxed("Copy").CallerBoxed(map, [s, g])!; };
            });

            return (TTarget)copyFunc(source, graph);
        }

        /// <summary>
        /// Creates a copy of the source object populating the members.
        /// Matching names are automatically mapped.
        /// Customizations are made by implementing <see cref="IMapDefinition{TSource, TTarget}"/>
        /// </summary>
        /// <typeparam name="TTarget">The type that will be copied.</typeparam>
        /// <param name="source">The source object for the copy.</param>
        /// <param name="graph">A graph to filter which properties to copy.</param>
        /// <returns>The new object with members populated.</returns>
        public static TTarget Copy<TTarget>(this TTarget source, Graph? graph = null)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            var sourceType = source.GetType();
            var targetType = typeof(TTarget);

            var key = new TypeKey(sourceType, targetType);
            var copyFunc = copyFuncs.GetOrAdd(key, sourceType, targetType, static (key, sourceType, targetType) =>
            {
                var genericMapType = TypeAnalyzer.GetGenericTypeDetail(mapType, sourceType, targetType);
                var map = genericMapType.GetMethodBoxed("GetMap").CallerBoxed(null, null);
                return (s, g) => { return genericMapType.GetMethodBoxed("Copy").CallerBoxed(map, [s, g])!; };
            });

            return (TTarget)copyFunc(source, graph);
        }

        /// <summary>
        /// Populates the members of the target object from the source object.
        /// Matching names are automatically mapped.
        /// Customizations are made by implementing <see cref="IMapDefinition{TSource, TTarget}"/>
        /// </summary>
        /// <typeparam name="TTarget">The type that will be copied.</typeparam>
        /// <param name="source">The source object for the copy.</param>
        /// <param name="graph">A graph to filter which properties to copy.</param>
        public static void CopyTo<TTarget>(this object source, TTarget target, Graph? graph = null)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (target is null)
                throw new ArgumentNullException(nameof(target));

            var sourceType = source.GetType();
            var targetType = typeof(TTarget);

            var key = new TypeKey(sourceType, targetType);
            var copyToFunc = copyToFuncs.GetOrAdd(key, sourceType, targetType, static (sourceType, targetType) =>
            {
                var genericMapType = TypeAnalyzer.GetGenericTypeDetail(mapType, sourceType, targetType);
                var map = genericMapType.GetMethodBoxed("GetMap").CallerBoxed(null, null);
                return (s, t, g) => { return genericMapType.GetMethodBoxed("CopyTo").CallerBoxed(map, [s, t, g])!; };
            });

            _ = copyToFunc(source, target, graph);
        }
    }
}
