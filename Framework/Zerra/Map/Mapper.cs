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
        private static readonly ConcurrentFactoryDictionary<TypeKey, Func<object, Graph?, object>> copyFuncs = new();
        private static readonly ConcurrentFactoryDictionary<TypeKey, Func<object, object, Graph?, object>> copyToFuncs = new();

        public static TTarget Map<TSource, TTarget>(this TSource source) { return Map<TSource, TTarget>(source, null); }
        public static TTarget Map<TSource, TTarget>(this TSource source, Graph? graph)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var map = Zerra.Map<TSource, TTarget>.GetMap();
            return map.Copy(source, graph);
        }

        public static void MapTo<TSource, TTarget>(this TSource source, TTarget target) { MapTo(source, target, null); }
        public static void MapTo<TSource, TTarget>(this TSource source, TTarget target, Graph? graph)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            var map = Zerra.Map<TSource, TTarget>.GetMap();
            _ = map.CopyTo(source, target, graph);
        }

        public static TTarget Map<TTarget>(this object source) { return Map<TTarget>(source, null); }
        public static TTarget Map<TTarget>(this object source, Graph? graph)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var key = new TypeKey(source.GetType(), typeof(TTarget));
            var copyFunc = copyFuncs.GetOrAdd(key, (key) =>
            {
                var genericMapType = TypeAnalyzer.GetGenericTypeDetail(mapType, key.Type1, key.Type2);
                var map = genericMapType.GetMethod("GetMap").Caller(null, null);
#pragma warning disable CS8603 // Possible null reference return.
                return (s, g) => { return genericMapType.GetMethod("Copy").Caller(map, new object?[] { s, g }); };
#pragma warning restore CS8603 // Possible null reference return.
            });

            return (TTarget)copyFunc(source, graph);
        }

        public static TTarget Copy<TTarget>(this TTarget source) { return Copy(source, null); }
        public static TTarget Copy<TTarget>(this TTarget source, Graph? graph)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var key = new TypeKey(typeof(TTarget), typeof(TTarget));
            var copyFunc = copyFuncs.GetOrAdd(key, (key) =>
            {
                var genericMapType = TypeAnalyzer.GetGenericTypeDetail(mapType, key.Type1, key.Type2);
                var map = genericMapType.GetMethod("GetMap").Caller(null, null);
#pragma warning disable CS8603 // Possible null reference return.
                return (s, g) => { return genericMapType.GetMethod("Copy").Caller(map, new object?[] { s, g }); };
#pragma warning restore CS8603 // Possible null reference return.
            });

            return (TTarget)copyFunc(source, graph);
        }

        public static void CopyTo<TTarget>(this object source, TTarget target) { CopyTo(source, target, null); }
        public static void CopyTo<TTarget>(this object source, TTarget target, Graph? graph)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            var key = new TypeKey(source.GetType(), typeof(TTarget));
            var copyToFunc = copyToFuncs.GetOrAdd(key, (key) =>
            {
                var genericMapType = TypeAnalyzer.GetGenericTypeDetail(mapType, key.Type1, key.Type2);
                var map = genericMapType.GetMethod("GetMap").Caller(null, null);
#pragma warning disable CS8603 // Possible null reference return.
                return (s, t, g) => { return genericMapType.GetMethod("CopyTo").Caller(map, new object?[] { s, t, g }); };
#pragma warning restore CS8603 // Possible null reference return.
            });

            _ = copyToFunc(source, target, graph);
        }
    }
}
