// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;
using Zerra.Collections;
using Zerra.Map.Converters;

namespace Zerra.Map
{
    public static class MapCustomizations
    {
        private readonly static ConcurrentFactoryDictionary<TypePairKey, object> customMapsByPair = new();

        internal static MapNameAndDeletage<TSource, TTarget>[]? Get<TSource, TTarget>()
        {
            var key = new TypePairKey(typeof(TSource), typeof(TTarget));
            if (customMapsByPair.TryGetValue(key, out var customMap))
                return (MapNameAndDeletage<TSource, TTarget>[])customMap;
            return null;
        }

        public static void Register<TSource, TTarget>(IMapDefinition<TSource, TTarget> mapDefinition)
        {
            MapConverterFactory.RegisterCreator<TSource, TTarget>();
            MapConverterFactory.RegisterCreator<TTarget, TSource>();

            var customizations = new MapCustomizationCollector<TSource, TTarget>();
            mapDefinition.Define(customizations);

            var keyNormal = new TypePairKey(typeof(TSource), typeof(TTarget));
            var keyReverse = new TypePairKey(typeof(TTarget), typeof(TSource));

            if (customizations.Results.Count == 0)
            {
                _ = customMapsByPair.TryAdd(keyNormal, Array.Empty<MapNameAndDeletage<TSource, TTarget>>());
                _ = customMapsByPair.TryAdd(keyReverse, Array.Empty<MapNameAndDeletage<TTarget, TSource>>());
            }

            var delegatesNormal = new List<MapNameAndDeletage<TSource, TTarget>>();
            var delegatesReverse = new List<MapNameAndDeletage<TTarget, TSource>>();

            var targetType = typeof(TTarget);

            var i = 0;
            foreach (var result in customizations.Results)
            {
                ParameterExpression sourceParameter;
                ParameterExpression targetParameter;
                Expression sourceMember;
                Expression targetMember;

                if (!result.IsReverse)
                {
                    sourceParameter = result.Source.Parameters[0];
                    targetParameter = result.Target.Parameters[0];
                    sourceMember = result.Source.Body;
                    targetMember = result.Target.Body;
                }
                else
                {
                    sourceParameter = result.Target.Parameters[0];
                    targetParameter = result.Source.Parameters[0];
                    sourceMember = result.Target.Body;
                    targetMember = result.Source.Body;
                }

                if (sourceMember.NodeType == ExpressionType.Convert)
                    sourceMember = ((UnaryExpression)sourceMember).Operand;

                if (targetMember.NodeType == ExpressionType.Convert)
                    targetMember = ((UnaryExpression)targetMember).Operand;

                var assignExpression = Expression.Assign(targetMember, sourceMember);

                string? targetName = null;
                if (targetMember is MemberExpression memberExpression)
                    targetName = memberExpression.Member.Name;

                if (targetParameter.Type == targetType)
                {
                    var lambda = Expression.Lambda<Action<TSource, TTarget>>(assignExpression, sourceParameter, targetParameter);
                    var compiled = lambda.Compile();
                    delegatesNormal.Add(new MapNameAndDeletage<TSource, TTarget>(targetName, compiled));
                }
                else
                {
                    var lambda = Expression.Lambda<Action<TTarget, TSource>>(assignExpression, sourceParameter, targetParameter);
                    var compiled = lambda.Compile();
                    delegatesReverse.Add(new MapNameAndDeletage<TTarget, TSource>(targetName, compiled));
                }
            }

            _ = customMapsByPair.TryAdd(keyNormal, delegatesNormal.ToArray());
            _ = customMapsByPair.TryAdd(keyReverse, delegatesReverse.ToArray());
        }
    }
}
