// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace Zerra.Repository
{
    /// <summary>
    /// The elements of a query given as they're read, cancelled by the token it was made with and by one given when it's read.
    /// </summary>
    internal sealed class RepoQueryStream<T> : IAsyncEnumerable<T>
    {
        private readonly RepoQueryProvider provider;
        private readonly Expression expression;
        private readonly CancellationToken cancellationToken;

        public RepoQueryStream(RepoQueryProvider provider, Expression expression, CancellationToken cancellationToken)
        {
            this.provider = provider;
            this.expression = expression;
            this.cancellationToken = cancellationToken;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            if (!cancellationToken.CanBeCanceled || cancellationToken == this.cancellationToken)
                return provider.ExecuteStreamAsync<T>(expression, this.cancellationToken).GetAsyncEnumerator(this.cancellationToken);
            if (!this.cancellationToken.CanBeCanceled)
                return provider.ExecuteStreamAsync<T>(expression, cancellationToken).GetAsyncEnumerator(cancellationToken);
            return Linked(this.cancellationToken).GetAsyncEnumerator(cancellationToken);
        }

        private async IAsyncEnumerable<T> Linked([EnumeratorCancellation] CancellationToken linked)
        {
            //the generated enumerator links the token given when it's read into the one it was made with
            await foreach (var element in provider.ExecuteStreamAsync<T>(expression, linked).WithCancellation(linked))
                yield return element;
        }
    }
}
