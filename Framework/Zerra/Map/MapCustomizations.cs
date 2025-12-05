// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Collections;

namespace Zerra.Map
{
    internal static class MapCustomizations
    {
        private readonly static ConcurrentFactoryDictionary<TypePairKey, object> mapSetups = new();

        public static MapSetup<TSource, TTarget>? GetMapSetup<TSource, TTarget>()
        {
            var key = new TypePairKey(typeof(TSource), typeof(TTarget));
            if (mapSetups.TryGetValue(key, out var mapDefinition))
                return (MapSetup<TSource, TTarget>)mapDefinition;
            return null;
        }

        public static void Register<TSource, TTarget>(IMapDefinition<TSource, TTarget> mapDefinition)
        {
            var mapSetup = new MapSetup<TSource, TTarget>();
            mapDefinition.Define(mapSetup);
            var key = new TypePairKey(typeof(TSource), typeof(TTarget));
            mapSetups[key] = mapSetup;
        }
    }
}
