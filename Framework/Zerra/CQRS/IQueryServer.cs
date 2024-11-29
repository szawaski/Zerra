// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Threading.Tasks;

namespace Zerra.CQRS
{
    /// <summary>
    /// Defines a query server that can receive, process and return query results.
    /// The implementation may also inherit <see cref="IDisposable"/> or <see cref="IAsyncDisposable "/> and the <see cref="Bus"/> will call dispose.
    /// </summary>
    public interface IQueryServer
    {
        /// <summary>
        /// The service url.
        /// </summary>
        string ServiceUrl { get; }
        /// <summary>
        /// Registers a query interface for this server.
        /// </summary>
        /// <param name="maxConcurrent">The max number of concurrent requests for this query interface.</param>
        /// <param name="type">The query interface type.</param>
        void RegisterInterfaceType(int maxConcurrent, Type type);
        /// <summary>
        /// A method called from <see cref="Bus"/> on startup to provide parts needed for the server.
        /// </summary>
        /// <param name="commandCounter">A counter to track and limit requests.</param>
        /// <param name="providerHandlerAsync">The hander delegate router that will link the acutal query methods.</param>
        void Setup(CommandCounter commandCounter, QueryHandlerDelegate providerHandlerAsync);
        /// <summary>
        /// A method called from <see cref="Bus"/> to start hosting.
        /// </summary>
        void Open();
        /// <summary>
        /// A method called from <see cref="Bus"/> to stop hosting.
        /// </summary>
        void Close();
    }

    public delegate Task<RemoteQueryCallResponse> QueryHandlerDelegate(Type interfaceName, string methodName, string?[] arguments, string source, bool isApi);
}