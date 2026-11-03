// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using System.Linq.Expressions;

namespace Zerra.Repository.Linq
{
    internal sealed class RepoQueryable<T> : IOrderedQueryable<T>, IAsyncEnumerable<T>
    {
        private readonly RepoQueryProvider provider;

        public RepoQueryable(RepoQueryProvider provider, Expression? expression)
        {
            this.provider = provider;
            this.Expression = expression ?? System.Linq.Expressions.Expression.Constant(this, typeof(IQueryable<T>));
        }

        public Type ElementType => typeof(T);
        public Expression Expression { get; }
        public IQueryProvider Provider => provider;

        public IEnumerator<T> GetEnumerator() => provider.ExecuteSequence<T>(Expression).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) => provider.ExecuteStreamAsync<T>(Expression, cancellationToken).GetAsyncEnumerator(cancellationToken);
    }
}
