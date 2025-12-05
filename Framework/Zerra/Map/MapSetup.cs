// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;

namespace Zerra.Map
{
    internal sealed class MapSetup<TSource, TTarget> : IMapSetup<TSource, TTarget>
    {
        public readonly Dictionary<Expression, Expression> CustomMappings = new();

        public void Define(Expression<Func<TTarget, object?>> property, Expression<Func<TSource, object?>> value)
        {
            CustomMappings.Add(property, value);
        }

        public void DefineTwoWay(Expression<Func<TTarget, object?>> property, Expression<Func<TSource, object?>> sourceProperty)
        {
            CustomMappings.Add(property, sourceProperty);
            CustomMappings.Add(sourceProperty, property);
        }
    }
}
