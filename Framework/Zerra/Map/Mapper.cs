// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.Collections;
using Zerra.Reflection;

namespace Zerra.Map
{
    public static class Mapper
    {
        public static bool DebugMode { get; set; } = Config.IsDebugBuild;

        private static readonly Type mapType = typeof(MapGenerator<,>);
        private static readonly ConcurrentFactoryDictionary<TypeKey, Func<object, Graph?, object>> copyFuncs = new();
        private static readonly ConcurrentFactoryDictionary<TypeKey, Func<object, object, Graph?, object>> copyToFuncs = new();

        public static TTarget Map<TSource, TTarget>(this TSource source) { return Map<TSource, TTarget>(source, null); }
        public static TTarget Map<TSource, TTarget>(this TSource source, Graph? graph)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var map = Zerra.Map.MapGenerator<TSource, TTarget>.GetMap();
            return map.Copy(source, graph);
        }

        public static void MapTo<TSource, TTarget>(this TSource source, TTarget target) { MapTo(source, target, null); }
        public static void MapTo<TSource, TTarget>(this TSource source, TTarget target, Graph? graph)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            var map = Zerra.Map.MapGenerator<TSource, TTarget>.GetMap();
            _ = map.CopyTo(source, target, graph);
        }

        public static TTarget Map<TTarget>(this object source) { return Map<TTarget>(source, null); }
        public static TTarget Map<TTarget>(this object source, Graph? graph)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var sourceType = source.GetType();
            var targetType = typeof(TTarget);

            var key = new TypeKey(sourceType, targetType);
            var copyFunc = copyFuncs.GetOrAdd(key, (key) =>
            {
                var genericMapType = TypeAnalyzer.GetGenericTypeDetail(mapType, sourceType, targetType);
                var map = genericMapType.GetMethodBoxed("GetMap").CallerBoxed(null, null);
                return (s, g) => { return genericMapType.GetMethodBoxed("Copy").CallerBoxed(map, new object?[] { s, g })!; };
            });

            return (TTarget)copyFunc(source, graph);
        }

        public static TTarget Copy<TTarget>(this TTarget source) { return Copy(source, null); }
        public static TTarget Copy<TTarget>(this TTarget source, Graph? graph)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var sourceType = source.GetType();
            var targetType = typeof(TTarget);

            var key = new TypeKey(sourceType, targetType);
            var copyFunc = copyFuncs.GetOrAdd(key, (key) =>
            {
                var genericMapType = TypeAnalyzer.GetGenericTypeDetail(mapType, sourceType, targetType);
                var map = genericMapType.GetMethodBoxed("GetMap").CallerBoxed(null, null);
                return (s, g) => { return genericMapType.GetMethodBoxed("Copy").CallerBoxed(map, new object?[] { s, g })!; };
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

            var sourceType = source.GetType();
            var targetType = typeof(TTarget);

            var key = new TypeKey(sourceType, targetType);
            var copyToFunc = copyToFuncs.GetOrAdd(key, (key) =>
            {
                var genericMapType = TypeAnalyzer.GetGenericTypeDetail(mapType, sourceType, targetType);
                var map = genericMapType.GetMethodBoxed("GetMap").CallerBoxed(null, null);
                return (s, t, g) => { return genericMapType.GetMethodBoxed("CopyTo").CallerBoxed(map, new object?[] { s, t, g })!; };
            });

            _ = copyToFunc(source, target, graph);
        }
    }
}
