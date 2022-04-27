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
        private static readonly ConcurrentFactoryDictionary<TypeKey, Func<object, IMapLogger, Graph, object>> copyFuncs = new ConcurrentFactoryDictionary<TypeKey, Func<object, IMapLogger, Graph, object>>();
        private static readonly ConcurrentFactoryDictionary<TypeKey, Func<object, object, IMapLogger, Graph, object>> copyToFuncs = new ConcurrentFactoryDictionary<TypeKey, Func<object, object, IMapLogger, Graph, object>>();

        public static TTarget Map<TSource, TTarget>(this TSource source, IMapLogger logger) { return Map<TSource, TTarget>(source, logger, null); }
        public static TTarget Map<TSource, TTarget>(this TSource source, IMapLogger logger, Graph graph)
        {
            if (source == null)
                return default;
            var map = Zerra.MapWithLog<TSource, TTarget>.GetMap();
            return map.Copy(source, logger, graph);
        }

        public static void MapTo<TSource, TTarget>(this TSource source, TTarget target, IMapLogger logger) { MapTo(source, target, logger, null); }
        public static void MapTo<TSource, TTarget>(this TSource source, TTarget target, IMapLogger logger, Graph graph)
        {
            if (source == null)
                return;
            var map = Zerra.MapWithLog<TSource, TTarget>.GetMap();
            map.CopyTo(source, target, logger, graph);
        }

        public static TTarget Map<TTarget>(this object source, IMapLogger logger) { return Map<TTarget>(source, logger, null); }
        public static TTarget Map<TTarget>(this object source, IMapLogger logger, Graph graph)
        {
            if (source == null)
                return default;
            var key = new TypeKey(source.GetType(), typeof(TTarget));
            var copyFunc = copyFuncs.GetOrAdd(key, (k) =>
            {
                var genericMapType = TypeAnalyzer.GetGenericTypeDetail(mapType, k.Type1, k.Type2);
                var map = genericMapType.GetMethod("GetMap").Caller(null, null);
                return (s, l, g) => { return genericMapType.GetMethod("Copy").Caller(map, new object[] { s, l, g }); };
            });

            return (TTarget)copyFunc(source, logger, graph);
        }

        public static TTarget Copy<TTarget>(this TTarget source, IMapLogger logger) { return Copy<TTarget>(source, logger, null); }
        public static TTarget Copy<TTarget>(this TTarget source, IMapLogger logger, Graph graph)
        {
            if (source == null)
                return default;
            var key = new TypeKey(typeof(TTarget), typeof(TTarget));
            var copyFunc = copyFuncs.GetOrAdd(key, (k) =>
            {
                var genericMapType = TypeAnalyzer.GetGenericTypeDetail(mapType, k.Type1, k.Type2);
                var map = genericMapType.GetMethod("GetMap").Caller(null, null);
                return (s, l, g) => { return genericMapType.GetMethod("Copy").Caller(map, new object[] { s, l, g }) ; };
            });

            return (TTarget)copyFunc(source, logger, graph);
        }

        public static void CopyTo<TTarget>(this object source, TTarget target, IMapLogger logger) { CopyTo(source, target, logger, null); }
        public static void CopyTo<TTarget>(this object source, TTarget target, IMapLogger logger, Graph graph)
        {
            if (source == null)
                return;
            var key = new TypeKey(source.GetType(), typeof(TTarget));
            var copyToFunc = copyToFuncs.GetOrAdd(key, (k) =>
            {
                var genericMapType = TypeAnalyzer.GetGenericTypeDetail(mapType, k.Type1, k.Type2);
                var map = genericMapType.GetMethod("GetMap").Caller(null, null);
                return (s, t, l, g) => { return genericMapType.GetMethod("CopyTo").Caller(map, new object[] { s, t, l, g }); };
            });

            copyToFunc(source, target, logger, graph);
        }
    }
}
