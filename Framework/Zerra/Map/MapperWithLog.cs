// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.Collections;
using Zerra.Reflection;

namespace Zerra.Map
{
    public static class MapperWithLog
    {
        public static bool DebugMode { get; set; } = false;

        private static readonly Type mapType = typeof(MapGeneratorWithLog<,>);
        private static readonly ConcurrentFactoryDictionary<TypeKey, Func<object, IMapLogger, Graph?, object>> copyFuncs = new();
        private static readonly ConcurrentFactoryDictionary<TypeKey, Func<object, object, IMapLogger, Graph?, object>> copyToFuncs = new();

        public static TTarget Map<TSource, TTarget>(this TSource source, IMapLogger logger) { return source.Map<TSource, TTarget>(logger, null); }
        public static TTarget Map<TSource, TTarget>(this TSource source, IMapLogger logger, Graph? graph)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var map = MapGeneratorWithLog<TSource, TTarget>.GetMap();
            return map.Copy(source, logger, graph);
        }

        public static void MapTo<TSource, TTarget>(this TSource source, TTarget target, IMapLogger logger) { source.MapTo(target, logger, null); }
        public static void MapTo<TSource, TTarget>(this TSource source, TTarget target, IMapLogger logger, Graph? graph)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            var map = MapGeneratorWithLog<TSource, TTarget>.GetMap();
            _ = map.CopyTo(source, target, logger, graph);
        }

        public static TTarget Map<TTarget>(this object source, IMapLogger logger) { return source.Map<TTarget>(logger, null); }
        public static TTarget Map<TTarget>(this object source, IMapLogger logger, Graph? graph)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var sourceType = source.GetType();
            var targetType = typeof(TTarget);

            var key = new TypeKey(sourceType, targetType);
            var copyFunc = copyFuncs.GetOrAdd(key, (key) =>
            {
                var genericMapType = TypeAnalyzer.GetGenericTypeDetail(mapType, sourceType, targetType);
                var map = genericMapType.GetMethodBoxed("GetMap").Caller(null, null);
                return (s, l, g) => { return genericMapType.GetMethodBoxed("Copy").Caller(map, new object?[] { s, l, g })!; };
            });

            return (TTarget)copyFunc(source, logger, graph);
        }

        public static TTarget Copy<TTarget>(this TTarget source, IMapLogger logger) { return source.Copy(logger, null); }
        public static TTarget Copy<TTarget>(this TTarget source, IMapLogger logger, Graph? graph)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var sourceType = source.GetType();
            var targetType = typeof(TTarget);

            var key = new TypeKey(sourceType, targetType);
            var copyFunc = copyFuncs.GetOrAdd(key, (key) =>
            {
                var genericMapType = TypeAnalyzer.GetGenericTypeDetail(mapType, sourceType, targetType);
                var map = genericMapType.GetMethodBoxed("GetMap").Caller(null, null);
                return (s, l, g) => { return genericMapType.GetMethodBoxed("Copy").Caller(map, new object?[] { s, l, g })!; };
            });

            return (TTarget)copyFunc(source, logger, graph);
        }

        public static void CopyTo<TTarget>(this object source, TTarget target, IMapLogger logger) { source.CopyTo(target, logger, null); }
        public static void CopyTo<TTarget>(this object source, TTarget target, IMapLogger logger, Graph? graph)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            var sourceType = source.GetType();
            var targetType = typeof(TTarget);

            var key = new TypeKey(sourceType, targetType);
            var copyToFunc = copyToFuncs.GetOrAdd(key, (key) =>
            {
                var genericMapType = TypeAnalyzer.GetGenericTypeDetail(mapType, sourceType, targetType);
                var map = genericMapType.GetMethodBoxed("GetMap").Caller(null, null);
                return (s, t, l, g) => { return genericMapType.GetMethodBoxed("CopyTo").Caller(map, new object?[] { s, t, l, g })!; };
            });

            _ = copyToFunc(source, target, logger, graph);
        }
    }
}
