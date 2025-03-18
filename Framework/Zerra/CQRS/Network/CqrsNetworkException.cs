// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.CQRS.Network
{
    /// <summary>
    /// A error occured between CQRS services
    /// </summary>
    public class CqrsNetworkException : Exception
    {
        /// <summary>
        /// Creates a new Exception with the default error message.
        /// </summary>
        public CqrsNetworkException() : base("A network error occured") { }
        /// <summary>
        /// Creates a new Exception with the specified error message.
        /// </summary>
        /// <param name="message">The exception message.</param>
        public CqrsNetworkException(string message) : base(message) { }
        /// <summary>
        /// Creates a new Exception with the default error message and a specified inner exception.
        /// </summary>
        /// <param name="innerException">The inner exception that caused the failure.</param>
        public CqrsNetworkException(Exception innerException) : base("A network error occured") { }
        /// <summary>
        /// Creates a new Exception with the specified error message and a specified inner exception.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="innerException">The inner exception that caused the failure.</param>
        public CqrsNetworkException(string message, Exception innerException) : base("A network error occured", innerException) { }
    }
}
