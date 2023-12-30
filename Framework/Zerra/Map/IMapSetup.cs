// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Linq.Expressions;

namespace Zerra
{
    public interface IMapSetup<TSource, TTarget>
    {
        void Define(Expression<Func<TTarget, object?>> property, Expression<Func<TSource, object?>> value);
        void DefineTwoWay(Expression<Func<TTarget, object?>> property, Expression<Func<TSource, object?>> sourceProperty);
        void Undefine(Expression<Func<TTarget, object?>> property);
        void UndefineTwoWay(Expression<Func<TTarget, object?>> property, Expression<Func<TSource, object?>> sourceProperty);
        void UndefineAll();
    }
}
