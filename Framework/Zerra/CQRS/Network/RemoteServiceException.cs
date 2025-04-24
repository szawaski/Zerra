// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.CQRS.Network
{
    /// <summary>
    /// An exception was thrown within a remote query or command.
    /// Check the inner exception if it was able to be deserialized.
    /// If the calling assembly does not have the Exception type thrown, it cannot be deserialized.
    /// </summary>
    public class RemoteServiceException : Exception
    {
        /// <summary>
        /// Creates a new exception with just the error text.
        /// </summary>
        /// <param name="message">The error text.</param>
        public RemoteServiceException(string? message)
            : base(message)
        { }

        /// <summary>
        /// Creates a new exception with the error text and the actual inner exception.
        /// </summary>
        /// <param name="message">The error text.</param>
        /// <param name="innerException">The actual inner exception.</param>
        public RemoteServiceException(string? message, Exception? innerException) 
            : base(message, innerException)
        { }
    }
}