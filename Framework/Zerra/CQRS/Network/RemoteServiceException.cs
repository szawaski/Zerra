// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.CQRS.Network
{
    /// <summary>
    /// An exception was thrown from a remote service query or command.
    /// </summary>
    public class RemoteServiceException : Exception
    {
        private readonly string? errorType;
        private readonly string? source;
        private readonly string? stackTrace;

        /// <summary>
        /// Creates a new exception with just the error text.
        /// </summary>
        /// <param name="source">The source of the error.</param>
        /// <param name="message">The error text.</param>
        public RemoteServiceException(string? source, string? message)
            : base(message)
        {
            this.errorType = null;
            this.source = source;
            this.stackTrace = null;
        }

        /// <summary>
        /// Creates a new exception with the error type, text, source, and stack trace. 
        /// </summary>
        /// <param name="errorType">The type of error that occurred as thrown on the remote service.</param>
        /// <param name="message">The error text.</param>
        /// <param name="source">The source of the error.</param>
        /// <param name="stackTrace">The stack trace of the error.</param>
        public RemoteServiceException(string errorType, string? message, string? source, string? stackTrace)
            : base(message)
        {
            this.errorType = errorType;
            this.source = source;
            this.stackTrace = stackTrace;
        }

        /// <summary>
        /// Gets the type of error that occurred as thrown on the remote service.
        /// AOT support prevents deserialization of the actual Exception type.
        /// </summary>
        public string? ErrorType => this.errorType;

        /// <inheritdoc />
        public override string? Source => this.source;

        /// <inheritdoc />
        public override string? StackTrace => this.stackTrace ?? base.StackTrace;

        /// <inheritdoc />
        public override string ToString()
        {
            if (this.errorType != null)
                return $"{source} - {errorType} - {base.ToString()}";
            return base.ToString();
        }
    }
}