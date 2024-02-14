// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.Collections;
using Zerra.Reflection;

namespace Zerra
{
    public static class MapperWithLog
    {
        public static bool DebugMode { get; set; } = false;

        private static readonly Type mapType = typeof(MapWithLog<,>);
        private static readonly ConcurrentFactoryDictionary<TypeKey, Func<object, IMapLogger, Graph?, object>> copyFuncs = new();
        private static readonly ConcurrentFactoryDictionary<TypeKey, Func<object, object, IMapLogger, Graph?, object>> copyToFuncs = new();

        public static TTarget Map<TSource, TTarget>(this TSource source, IMapLogger logger) { return Map<TSource, TTarget>(source, logger, null); }
        public static TTarget Map<TSource, TTarget>(this TSource source, IMapLogger logger, Graph? graph)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var map = Zerra.MapWithLog<TSource, TTarget>.GetMap();
            return map.Copy(source, logger, graph);
        }

        public static void MapTo<TSource, TTarget>(this TSource source, TTarget target, IMapLogger logger) { MapTo(source, target, logger, null); }
        public static void MapTo<TSource, TTarget>(this TSource source, TTarget target, IMapLogger logger, Graph? graph)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            var map = Zerra.MapWithLog<TSource, TTarget>.GetMap();
            _ = map.CopyTo(source, target, logger, graph);
        }

        public static TTarget Map<TTarget>(this object source, IMapLogger logger) { return Map<TTarget>(source, logger, null); }
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
                var map = genericMapType.GetMethod("GetMap").Caller(null, null);
                return (s, l, g) => { return genericMapType.GetMethod("Copy").Caller(map, new object?[] { s, l, g })!; };
            });

            return (TTarget)copyFunc(source, logger, graph);
        }

        public static TTarget Copy<TTarget>(this TTarget source, IMapLogger logger) { return Copy<TTarget>(source, logger, null); }
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
                var map = genericMapType.GetMethod("GetMap").Caller(null, null);
                return (s, l, g) => { return genericMapType.GetMethod("Copy").Caller(map, new object?[] { s, l, g })!; };
            });

            return (TTarget)copyFunc(source, logger, graph);
        }

        public static void CopyTo<TTarget>(this object source, TTarget target, IMapLogger logger) { CopyTo(source, target, logger, null); }
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
                var map = genericMapType.GetMethod("GetMap").Caller(null, null);
                return (s, t, l, g) => { return genericMapType.GetMethod("CopyTo").Caller(map, new object?[] { s, t, l, g })!; };
            });

            _ = copyToFunc(source, target, logger, graph);
        }
    }
}
