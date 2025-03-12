// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.CQRS.Network
{
    /// <summary>
    /// A connection between CQRS services could not be established.
    /// </summary>
    public sealed class ConnectionFailedException : CqrsNetworkException
    {
        /// <summary>
        /// Creates a new Exception with the default error message.
        /// </summary>
        public ConnectionFailedException() : base("Failed to establish a connection") { }
        /// <summary>
        /// Creates a new Exception with the default error message and a specified inner exception.
        /// </summary>
        /// <param name="innerException">The inner exception that failed the connection.</param>
        public ConnectionFailedException(Exception innerException) : base("Failed to establish a connection", innerException) { }
        /// <summary>
        /// Creates a new Exception with the specified error message and a specified inner exception.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="innerException">The inner exception that failed the connection.</param>
        public ConnectionFailedException(string message, Exception innerException) : base(message, innerException) { }
    }
}
