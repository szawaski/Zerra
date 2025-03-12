// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.Collections;
using Zerra.Reflection;

namespace Zerra.Map
{
    /// <summary>
    /// Methods for mapping properties of a source object to a target object with a parameter for logging changes.
    /// </summary>
    public static class MapperWithLog
    {
        internal static bool DebugMode { get; set; } = Config.IsDebugBuild;

        private static readonly Type mapType = typeof(MapGeneratorWithLog<,>);
        private static readonly ConcurrentFactoryDictionary<TypeKey, Func<object, IMapLogger, Graph?, object>> copyFuncs = new();
        private static readonly ConcurrentFactoryDictionary<TypeKey, Func<object, object, IMapLogger, Graph?, object>> copyToFuncs = new();

        /// <summary>
        /// Creates a new object of the target type and populates the members from the source object.
        /// Type is found from the generic parameter.
        /// Matching names are automatically mapped and will attempt to convert types.
        /// Customizations are made by implementing <see cref="IMapDefinition{TSource, TTarget}"/>
        /// </summary>
        /// <typeparam name="TSource">The source type that will be mapped.</typeparam>
        /// <typeparam name="TTarget">The target type that will be mapped.</typeparam>
        /// <param name="source">The source object for the mapping.</param>
        /// <param name="logger">A logger instance to record the changes.</param>
        /// <param name="graph">A graph to filter which properties to map.</param>
        /// <returns>The new object of the target type with members populated from the source.</returns>
        public static TTarget Map<TSource, TTarget>(this TSource source, IMapLogger logger, Graph? graph = null)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            var map = MapGeneratorWithLog<TSource, TTarget>.GetMap();
            return map.Copy(source, logger, graph);
        }

        /// <summary>
        /// Populates the members of the target object from the source object.
        /// Type is found from the generic parameter.
        /// Matching names are automatically mapped and will attempt to convert types.
        /// Customizations are made by implementing <see cref="IMapDefinition{TSource, TTarget}"/>
        /// </summary>
        /// <typeparam name="TSource">The source type that will be mapped.</typeparam>
        /// <typeparam name="TTarget">The target type that will be mapped.</typeparam>
        /// <param name="source">The source object for the mapping.</param>
        /// <param name="target">The target object for the properies.</param>
        /// <param name="logger">A logger instance to record the changes.</param>
        /// <param name="graph">A graph to filter which properties to map.</param>
        public static void MapTo<TSource, TTarget>(this TSource source, TTarget target, IMapLogger logger, Graph? graph = null)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (target is null)
                throw new ArgumentNullException(nameof(target));

            var map = MapGeneratorWithLog<TSource, TTarget>.GetMap();
            _ = map.CopyTo(source, target, logger, graph);
        }

        /// <summary>
        /// Creates a new object of the target type and populates the members from the source object.
        /// Type is found using GetType.
        /// Matching names are automatically mapped and will attempt to convert types.
        /// Customizations are made by implementing <see cref="IMapDefinition{TSource, TTarget}"/>
        /// </summary>
        /// <typeparam name="TTarget">The target type that will be mapped.</typeparam>
        /// <param name="source">The source object for the mapping.</param>
        /// <param name="logger">A logger instance to record the changes.</param>
        /// <param name="graph">A graph to filter which properties to map.</param>
        /// <returns>The new object of the target type with members populated from the source.</returns>
        public static TTarget Map<TTarget>(this object source, IMapLogger logger, Graph? graph = null)
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
                return (s, l, g) => { return genericMapType.GetMethodBoxed("Copy").CallerBoxed(map, [s, l, g])!; };
            });

            return (TTarget)copyFunc(source, logger, graph);
        }

        /// <summary>
        /// Populates the members of the target object from the source object.
        /// Matching names are automatically mapped and will attempt to convert types.
        /// Customizations are made by implementing <see cref="IMapDefinition{TSource, TTarget}"/>
        /// </summary>
        /// <typeparam name="TTarget">The target type that will be mapped.</typeparam>
        /// <param name="source">The source object for the mapping.</param>
        /// <param name="target">The target object for the properies.</param>
        /// <param name="logger">A logger instance to record the changes.</param>
        /// <param name="graph">A graph to filter which properties to map.</param>
        public static void MapTo<TTarget>(this object source, TTarget target, IMapLogger logger, Graph? graph = null)
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
                return (s, t, l, g) => { return genericMapType.GetMethodBoxed("CopyTo").CallerBoxed(map, [s, t, l, g])!; };
            });

            _ = copyToFunc(source, target, logger, graph);
        }

        /// <summary>
        /// Creates a copy of the source object populating the members.
        /// Type is found from the generic parameter.
        /// Matching names are automatically mapped.
        /// Customizations are made by implementing <see cref="IMapDefinition{TSource, TTarget}"/>
        /// </summary>
        /// <typeparam name="TTarget">The type that will be copied.</typeparam>
        /// <param name="source">The source object for the copy.</param>
        /// <param name="logger">A logger instance to record the changes.</param>
        /// <param name="graph">A graph to filter which properties to copy.</param>
        /// <returns>The new object with members populated.</returns>
        public static TTarget Copy<TTarget>(this TTarget source, IMapLogger logger, Graph? graph = null)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            var type = typeof(TTarget);

            var key = new TypeKey(type, type);
            var copyFunc = copyFuncs.GetOrAdd(key, type, type, static (sourceType, targetType) =>
            {
                var genericMapType = TypeAnalyzer.GetGenericTypeDetail(mapType, sourceType, targetType);
                var map = genericMapType.GetMethodBoxed("GetMap").CallerBoxed(null, null);
                return (s, l, g) => { return genericMapType.GetMethodBoxed("Copy").CallerBoxed(map, [s, l, g])!; };
            });

            return (TTarget)copyFunc(source, logger, graph);
        }

        /// <summary>
        /// Creates a copy of the source object populating the members.
        /// Type is found using GetType.
        /// Matching names are automatically mapped.
        /// Customizations are made by implementing <see cref="IMapDefinition{TSource, TTarget}"/>
        /// </summary>
        /// <param name="source">The source object for the copy.</param>
        /// <param name="logger">A logger instance to record the changes.</param>
        /// <param name="graph">A graph to filter which properties to copy.</param>
        /// <returns>The new object with members populated.</returns>
        public static object Copy(this object source, IMapLogger logger, Graph? graph = null)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            var type = source.GetType();

            var key = new TypeKey(type, type);
            var copyFunc = copyFuncs.GetOrAdd(key, type, type, static (sourceType, targetType) =>
            {
                var genericMapType = TypeAnalyzer.GetGenericTypeDetail(mapType, sourceType, targetType);
                var map = genericMapType.GetMethodBoxed("GetMap").CallerBoxed(null, null);
                return (s, l, g) => { return genericMapType.GetMethodBoxed("Copy").CallerBoxed(map, [s, l, g])!; };
            });

            return copyFunc(source, logger, graph);
        }
    }
}
