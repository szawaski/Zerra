// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Collections;
using Zerra.Map.Converters;

namespace Zerra.Map
{
    /// <summary>
    /// Provides static methods to register and manage custom map definitions for type conversions.
    /// </summary>
    public static class MapDefinition
    {
        private readonly static ConcurrentFactoryDictionary<TypePairKey, object> customMapsByPair = new();

        internal static MapDefinitionInfo[]? Get<TSource, TTarget>()
        {
            var key = new TypePairKey(typeof(TSource), typeof(TTarget));
            if (customMapsByPair.TryGetValue(key, out var customMap))
                return (MapDefinitionInfo[])customMap;
            return null;
        }

        /// <summary>
        /// Registers a map definition for the specified source and target types.
        /// </summary>
        /// <typeparam name="TSource">The source type.</typeparam>
        /// <typeparam name="TTarget">The target type.</typeparam>
        /// <param name="mapDefinition">The map definition to register.</param>
        public static void Register<TSource, TTarget>(IMapDefinition<TSource, TTarget> mapDefinition)
        {
            Register<TSource, TTarget, TSource, TTarget, object, object, object, object>(mapDefinition);
        }

        /// <summary>
        /// Registers a map definition for the specified source and target types with support for collections and dictionaries.
        /// </summary>
        /// <typeparam name="TSource">The source type.</typeparam>
        /// <typeparam name="TTarget">The target type.</typeparam>
        /// <typeparam name="TSourceEnumerable">The enumerable inner type of the source, if applicable.</typeparam>
        /// <typeparam name="TTargetEnumerable">The enumerable inner type of the target, if applicable.</typeparam>
        /// <typeparam name="TSourceKey">The key type of the source dictionary, if applicable.</typeparam>
        /// <typeparam name="TSourceValue">The value type of the source dictionary, if applicable.</typeparam>
        /// <typeparam name="TTargetKey">The key type of the target dictionary, if applicable.</typeparam>
        /// <typeparam name="TTargetValue">The value type of the target dictionary, if applicable.</typeparam>
        /// <param name="mapDefinition">The map definition to register.</param>
        public static void Register<TSource, TTarget, TSourceEnumerable, TTargetEnumerable, TSourceKey, TSourceValue, TTargetKey, TTargetValue>(IMapDefinition<TSource, TTarget> mapDefinition)
            where TSourceKey : notnull
            where TTargetKey : notnull
        {
            MapConverterFactory.RegisterCreator<TSource, TTarget, TSourceEnumerable, TTargetEnumerable, TSourceKey, TSourceValue, TTargetKey, TTargetValue>();
            MapConverterFactory.RegisterCreator<TTarget, TSource, TTargetEnumerable, TSourceEnumerable, TTargetKey, TTargetValue, TSourceKey, TSourceValue>();

            var collector = new MapDefinitionBuilder<TSource, TTarget>();
            mapDefinition.Define(collector);

            var keyNormal = new TypePairKey(typeof(TSource), typeof(TTarget));
            var keyReverse = new TypePairKey(typeof(TTarget), typeof(TSource));

            if (collector.Results.Count == 0)
            {
                _ = customMapsByPair.TryAdd(keyNormal, Array.Empty<MapDefinitionInfo>());
                _ = customMapsByPair.TryAdd(keyReverse, Array.Empty<MapDefinitionInfo>());
                return;
            }

            var delegatesNormal = new List<MapDefinitionInfo>();
            var delegatesReverse = new List<MapDefinitionInfo>();

            if (customMapsByPair.TryGetValue(keyNormal, out var existingDelegatesNormal))
                delegatesNormal.AddRange((MapDefinitionInfo[])existingDelegatesNormal);
            if (customMapsByPair.TryGetValue(keyReverse, out var existingDelegatesReverse))
                delegatesReverse.AddRange((MapDefinitionInfo[])existingDelegatesReverse);

            var targetType = typeof(TTarget);

            foreach (var result in collector.Results)
            {
                if (!result.IsReverse)
                    delegatesNormal.Add(result);
                else
                    delegatesReverse.Add(result);
            }

            _ = customMapsByPair.TryAdd(keyNormal, delegatesNormal.ToArray());
            _ = customMapsByPair.TryAdd(keyReverse, delegatesReverse.ToArray());
        }
    }
}
