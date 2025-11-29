// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.CQRS.Network
{
    /// <summary>
    /// Defines a class used for authorization of externally exposed services on both client and server sides.
    /// Used with a CQRS API such as a Gateway
    /// </summary>
    public interface ICqrsAuthorizer
    {
        /// <summary>
        /// The server side validates that the incomming request is authorized.
        /// An Exception should be thrown to invalidate requests.
        /// </summary>
        /// <param name="headers">A dictionary of incomming headers.</param>
        void Authorize(Dictionary<string, List<string?>> headers);

        /// <summary>
        /// The client side adds headers needed by the server side to validate the request.
        /// </summary>
        /// <returns>A dictionary of outgoing headers.</returns>
        ValueTask<Dictionary<string, List<string?>>> GetAuthorizationHeadersAsync(CancellationToken cancellationToken = default);
    }
}