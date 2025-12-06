// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;

namespace Zerra.Map
{
    internal sealed class MapCustomizationCollector<TSource, TTarget> : IMapSetup<TSource, TTarget>
    {
        public readonly List<Tuple<Expression, Expression>> Results = new();

        public void Define(Expression<Func<TTarget, object?>> property, Expression<Func<TSource, object?>> value)
        {
            Results.Add(new(property, value));
        }

        public void DefineTwoWay(Expression<Func<TTarget, object?>> property1, Expression<Func<TSource, object?>> property2)
        {
            Results.Add(new(property1, property2));
            Results.Add(new(property2, property1));
        }
    }
}
