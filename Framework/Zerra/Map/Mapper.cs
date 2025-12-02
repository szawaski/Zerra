// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Runtime.CompilerServices;
using Zerra.Collections;

namespace Zerra.Map
{
    public static class Mapper
    {
        internal static bool DebugMode { get; set; } = false;

        private static readonly ConcurrentFactoryDictionary<TypePairKey, Delegate> mapCache = new();

        public static TTarget Map<TSource, TTarget>(this TSource source, Graph? graph = null)
            where TSource : notnull
            where TTarget : notnull
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            var map = GetMap<TSource, TTarget>();
            var result = map(source, default, graph);
            return result;
        }

        public static void MapTo<TSource, TTarget>(this TSource source, TTarget target, Graph? graph = null)
            where TSource : notnull
            where TTarget : notnull
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (target is null)
                throw new ArgumentNullException(nameof(target));

            var map = GetMap<TSource, TTarget>();
            _ = map(source, target, graph);
        }

        public static TTarget Copy<TTarget>(this TTarget source, Graph? graph = null)
            where TTarget : notnull
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            var map = GetMap<TTarget, TTarget>();
            var result = map(source, default, graph);
            return result;
        }

        private static Func<TSource, TTarget?, Graph?, TTarget> GetMap<TSource, TTarget>()
            where TSource : notnull
            where TTarget : notnull
        {
            var sourceType = typeof(TSource);
            var targetType = typeof(TTarget);
            var key = new TypePairKey(sourceType, targetType);
            var map = (Func<TSource, TTarget?, Graph?, TTarget>)mapCache.GetOrAdd(key, static () => Generate<TSource, TTarget>());
            return map;
        }

        private static Func<TSource, TTarget?, Graph?, TTarget> Generate<TSource, TTarget>()
            where TSource : notnull
            where TTarget : notnull
        {
            //if (RuntimeFeature.IsDynamicCodeSupported)
            //    return ReflectionMapGenerator.Generate<TSource, TTarget>();
            //else
                return InterpretedMapGenerator.Generate<TSource, TTarget>();
        }
    }
}
