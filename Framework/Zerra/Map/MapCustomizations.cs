// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;
using Zerra.Collections;

namespace Zerra.Map
{
    public static class MapCustomizations
    {
        private readonly static ConcurrentFactoryDictionary<TypePairKey, ConcurrentList<Delegate>> customMapsByPair = new();

        public static Action<TSource, TTarget>[]? Get<TSource, TTarget>()
        {
            var key = new TypePairKey(typeof(TSource), typeof(TTarget));
            if (customMapsByPair.TryGetValue(key, out var customMap))
                return customMap.Cast<Action<TSource, TTarget>>().ToArray();
            return null;
        }

        public static void Register<TSource, TTarget>(IMapDefinition<TSource, TTarget> mapDefinition)
        {
            var mapSetup = new MapCustomizationCollector<TSource, TTarget>();
            mapDefinition.Define(mapSetup);

            var keyNormal = new TypePairKey(typeof(TSource), typeof(TTarget));
            var delegatesNormal = customMapsByPair.GetOrAdd(keyNormal, () => new());

            var keyReverse = new TypePairKey(typeof(TTarget), typeof(TSource));
            var delegatesReverse = customMapsByPair.GetOrAdd(keyReverse, () => new());

            var targetType = typeof(TTarget);

            var i = 0;
            foreach (var result in mapSetup.Results)
            {
                var sourceLambda = (LambdaExpression)result.Item2;
                var targetLambda = (LambdaExpression)result.Item1;

                var sourceParameter = sourceLambda.Parameters[0];
                var targetParameter = targetLambda.Parameters[0];

                var sourceMember = sourceLambda.Body;
                var targetMember = targetLambda.Body;

                if (sourceMember.NodeType == ExpressionType.Convert)
                    sourceMember = ((UnaryExpression)sourceMember).Operand;

                if (targetMember.NodeType == ExpressionType.Convert)
                    targetMember = ((UnaryExpression)targetMember).Operand;

                var assignExpression = Expression.Assign(targetMember, sourceMember);

                if (targetParameter.Type == targetType)
                {
                    var lambda = Expression.Lambda<Action<TSource, TTarget>>(assignExpression, sourceParameter, targetParameter);
                    var compiled = lambda.Compile();
                    delegatesNormal.Add(compiled);
                }
                else
                {
                    var lambda = Expression.Lambda<Action<TTarget, TSource>>(assignExpression, sourceParameter, targetParameter);
                    var compiled = lambda.Compile();
                    delegatesReverse.Add(compiled);
                }
            }
        }
    }
}
