// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;

namespace Zerra.Repository
{
    public abstract class OrderExpression
    {
        public abstract Expression Expression { get; }
        public abstract bool Descending { get; }

        public abstract IOrderedQueryable<T> OrderBy<T>(IQueryable<T> source) where T : class, new();
    }
}