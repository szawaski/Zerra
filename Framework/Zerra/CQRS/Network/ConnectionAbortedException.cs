// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.CQRS.Network
{
    /// <summary>
    /// A connection between CQRS services was aborted.
    /// </summary>
    public sealed class ConnectionAbortedException : CqrsNetworkException
    {
        /// <summary>
        /// Creates a new Exception with the default error message.
        /// </summary>
        public ConnectionAbortedException() : base("Connection aborted while processing request") { }
        /// <summary>
        /// Creates a new Exception with the specified error message.
        /// </summary>
        /// <param name="message">The exception message.</param>
        public ConnectionAbortedException(string message) : base(message) { }
    }
}
