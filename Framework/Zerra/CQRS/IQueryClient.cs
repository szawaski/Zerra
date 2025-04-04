// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Threading;

namespace Zerra.CQRS
{
    /// <summary>
    /// Defines a query client that can send queries.
    /// </summary>
    public interface IQueryClient
    {
        /// <summary>
        /// The service url.
        /// </summary>
        string ServiceUrl { get; }
        /// <summary>
        /// Registers a query interface for this client.
        /// </summary>
        /// <param name="maxConcurrent">The max number of concurrent requests for this query interface.</param>
        /// <param name="type">The query interface type.</param>
        void RegisterInterfaceType(int maxConcurrent, Type type);
        /// <summary>
        /// Executes a query from a query interface method.
        /// </summary>
        /// <typeparam name="TReturn">The expected return type.</typeparam>
        /// <param name="interfaceType">The query interface type.</param>
        /// <param name="methodName">The method name on the query interface.</param>
        /// <param name="arguments">The arguments needed for the method.</param>
        /// <param name="source">A description of where the query came from.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The result of the query.</returns>
        TReturn? Call<TReturn>(Type interfaceType, string methodName, object[] arguments, string source, CancellationToken cancellationToken);
    }
}