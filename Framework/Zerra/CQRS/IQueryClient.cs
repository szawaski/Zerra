// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Encryption;

namespace Zerra.CQRS
{
    /// <summary>
    /// Defines a query client that can send queries.
    /// </summary>
    public interface IQueryClient : IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// The service url.
        /// </summary>
        string ServiceUrl { get; }
        /// <summary>
        /// Registers a query interface for this client.
        /// </summary>
        /// <param name="type">The query interface type.</param>
        void RegisterInterfaceType(Type type);
        /// <summary>
        /// Executes a query from a query interface method.
        /// </summary>
        /// <typeparam name="TReturn">The expected return type.</typeparam>
        /// <param name="interfaceType">The query interface type.</param>
        /// <param name="methodName">The method name on the query interface.</param>
        /// /<param name="argumentTypes">The types of the arguments needed for the method.</param>
        /// <param name="arguments">The arguments needed for the method.</param>
        /// <param name="source">A description of where the query came from.</param>
        /// <param name="overrideServiceUrl">The address of a specific server to call instead of the service url, or null.</param>
        /// <param name="overrideEncryptor">An encryptor to use instead of the client's for this call, or null.</param>
        /// <returns>The result of the query.</returns>
        TReturn Call<TReturn>(Type interfaceType, string methodName, IReadOnlyList<Type> argumentTypes, object[] arguments, string source, string? overrideServiceUrl, IEncryptor? overrideEncryptor);
        /// <summary>
        /// Executes a query from a query interface method asynchronously without returning a result.
        /// </summary>
        /// <param name="interfaceType">The query interface type.</param>
        /// <param name="methodName">The method name on the query interface.</param>
        /// <param name="argumentTypes">The types of the arguments needed for the method.</param>
        /// <param name="arguments">The arguments needed for the method.</param>
        /// <param name="source">A description of where the query came from.</param>
        /// <param name="overrideServiceUrl">The address of a specific server to call instead of the service url, or null.</param>
        /// <param name="overrideEncryptor">An encryptor to use instead of the client's for this call, or null.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous query operation.</returns>
        Task CallTask(Type interfaceType, string methodName, IReadOnlyList<Type> argumentTypes, object[] arguments, string source, string? overrideServiceUrl, IEncryptor? overrideEncryptor, CancellationToken cancellationToken);
        /// <summary>
        /// Executes a query from a query interface method asynchronously and returns a result.
        /// </summary>
        /// <typeparam name="TReturn">The expected return type.</typeparam>
        /// <param name="interfaceType">The query interface type.</param>
        /// <param name="methodName">The method name on the query interface.</param>
        /// <param name="argumentTypes">The types of the arguments needed for the method.</param>
        /// <param name="arguments">The arguments needed for the method.</param>
        /// <param name="source">A description of where the query came from.</param>
        /// <param name="overrideServiceUrl">The address of a specific server to call instead of the service url, or null.</param>
        /// <param name="overrideEncryptor">An encryptor to use instead of the client's for this call, or null.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous query operation. The task result contains the query result.</returns>
        Task<TReturn> CallTaskGeneric<TReturn>(Type interfaceType, string methodName, IReadOnlyList<Type> argumentTypes, object[] arguments, string source, string? overrideServiceUrl, IEncryptor? overrideEncryptor, CancellationToken cancellationToken);
    }
}