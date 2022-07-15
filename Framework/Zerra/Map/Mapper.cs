// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.Collections;
using Zerra.Reflection;

namespace Zerra
{
    public static class Mapper
    {
        public static bool DebugMode { get; set; } = Config.IsDebugBuild;

        private static readonly Type mapType = typeof(Map<,>);
        private static readonly ConcurrentFactoryDictionary<TypeKey, Func<object, Graph, object>> copyFuncs = new();
        private static readonly ConcurrentFactoryDictionary<TypeKey, Func<object, object, Graph, object>> copyToFuncs = new();

        public static TTarget Map<TSource, TTarget>(this TSource source) { return Map<TSource, TTarget>(source, null); }
        public static TTarget Map<TSource, TTarget>(this TSource source, Graph graph)
        {
            if (source == null)
                return default;
            var map = Zerra.Map<TSource, TTarget>.GetMap();
            return map.Copy(source, graph);
        }

        public static void MapTo<TSource, TTarget>(this TSource source, TTarget target) { MapTo(source, target, null); }
        public static void MapTo<TSource, TTarget>(this TSource source, TTarget target, Graph graph)
        {
            if (source == null)
                return;
            var map = Zerra.Map<TSource, TTarget>.GetMap();
            _ = map.CopyTo(source, target, graph);
        }

        public static TTarget Map<TTarget>(this object source) { return Map<TTarget>(source, null); }
        public static TTarget Map<TTarget>(this object source, Graph graph)
        {
            if (source == null)
                return default;
            var key = new TypeKey(source.GetType(), typeof(TTarget));
            var copyFunc = copyFuncs.GetOrAdd(key, (k) =>
            {
                var genericMapType = TypeAnalyzer.GetGenericTypeDetail(mapType, k.Type1, k.Type2);
                var map = genericMapType.GetMethod("GetMap").Caller(null, null);
                return (s, g) => { return genericMapType.GetMethod("Copy").Caller(map, new object[] { s, g }); };
            });

            return (TTarget)copyFunc(source, graph);
        }

        public static TTarget Copy<TTarget>(this TTarget source) { return Copy<TTarget>(source, null); }
        public static TTarget Copy<TTarget>(this TTarget source, Graph graph)
        {
            if (source == null)
                return default;
            var key = new TypeKey(typeof(TTarget), typeof(TTarget));
            var copyFunc = copyFuncs.GetOrAdd(key, (k) =>
            {
                var genericMapType = TypeAnalyzer.GetGenericTypeDetail(mapType, k.Type1, k.Type2);
                var map = genericMapType.GetMethod("GetMap").Caller(null, null);
                return (s, g) => { return genericMapType.GetMethod("Copy").Caller(map, new object[] { s, g }); };
            });

            return (TTarget)copyFunc(source, graph);
        }

        public static void CopyTo<TTarget>(this object source, TTarget target) { CopyTo(source, target, null); }
        public static void CopyTo<TTarget>(this object source, TTarget target, Graph graph)
        {
            if (source == null)
                return;
            var key = new TypeKey(source.GetType(), typeof(TTarget));
            var copyToFunc = copyToFuncs.GetOrAdd(key, (k) =>
            {
                var genericMapType = TypeAnalyzer.GetGenericTypeDetail(mapType, k.Type1, k.Type2);
                var map = genericMapType.GetMethod("GetMap").Caller(null, null);
                return (s, t, g) => { return genericMapType.GetMethod("CopyTo").Caller(map, new object[] { s, t, g }); };
            });

            _ = copyToFunc(source, target, graph);
        }
    }
}
