// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;

namespace Zerra.Map
{
    internal sealed class MapCustomizationCollector<TSource, TTarget> : IMapSetup<TSource, TTarget>
    {
        public readonly List<Result> Results = new();

        public void Define(Expression<Func<TTarget, object?>> property, Expression<Func<TSource, object?>> value)
        {
            Results.Add(new(false, property, value));
        }

        public void DefineTwoWay(Expression<Func<TTarget, object?>> property1, Expression<Func<TSource, object?>> property2)
        {
            Results.Add(new(false, property1, property2));
            Results.Add(new(false, property2, property1));
            Results.Add(new(true, property2, property1));
            Results.Add(new(true, property1, property2));
        }

        public void DefineReverse(Expression<Func<TSource, object?>> property, Expression<Func<TTarget, object?>> value)
        {
            Results.Add(new(true, property, value));
        }

        public sealed class Result
        {
            public bool IsReverse { get; }
            public LambdaExpression Target { get; }
            public LambdaExpression Source { get; }

            public Result(bool isReverse, LambdaExpression target, LambdaExpression source)
            {
                this.IsReverse = isReverse;
                this.Source = source;
                this.Target = target;
            }
        }
    }
}
