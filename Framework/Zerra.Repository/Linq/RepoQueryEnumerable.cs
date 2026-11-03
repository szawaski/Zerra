// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using System.Linq.Expressions;

namespace Zerra.Repository.Linq
{
    /// <summary>
    /// The elements of a query given as they're read, the query running each time they're read.
    /// </summary>
    internal sealed class RepoQueryEnumerable<T> : IEnumerable<T>
    {
        private readonly RepoQueryProvider provider;
        private readonly Expression expression;

        public RepoQueryEnumerable(RepoQueryProvider provider, Expression expression)
        {
            this.provider = provider;
            this.expression = expression;
        }

        public IEnumerator<T> GetEnumerator() => provider.ExecuteStream<T>(expression).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
