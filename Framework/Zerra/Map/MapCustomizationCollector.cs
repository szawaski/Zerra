// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;

namespace Zerra.Map
{
    internal sealed class MapCustomizationCollector<TSource, TTarget> : IMapSetup<TSource, TTarget>
    {
        public readonly List<Result> Results = new();

        public void Define<TValue>(Expression<Func<TTarget, TValue?>> property, Expression<Func<TSource, TValue?>> value)
        {
            Results.Add(new(property, value));
        }

        public void DefineTwoWay<TValue>(Expression<Func<TTarget, TValue?>> property1, Expression<Func<TSource, TValue?>> property2)
        {
            Results.Add(new(property1, property2));
            Results.Add(new(property2, property1));
        }

        public void DefineReverse<TValue>(Expression<Func<TSource, TValue?>> property, Expression<Func<TTarget, TValue?>> value)
        {
            Results.Add(new(property, value));
        }

        public sealed class Result
        {
            public LambdaExpression Target { get; }
            public LambdaExpression Source { get; }

            public Result(LambdaExpression target, LambdaExpression source)
            {
                this.Source = source;
                this.Target = target;
            }
        }
    }
}
