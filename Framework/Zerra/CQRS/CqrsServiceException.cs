// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.CQRS
{
    /// <summary>
    /// An exception was thrown from a service query or command.
    /// </summary>
    public class CqrsServiceException : Exception
    {
        private readonly string errorType;
        private readonly string? source;
        private readonly string? stackTrace;

        /// <summary>
        /// Creates a new exception with the error type, text, source, and stack trace. 
        /// </summary>
        /// <param name="errorType">The type of error that occurred as thrown on the remote service.</param>
        /// <param name="message">The error text.</param>
        /// <param name="source">The source of the error.</param>
        /// <param name="stackTrace">The stack trace of the error.</param>
        public CqrsServiceException(string errorType, string? message, string? source, string? stackTrace)
            : base(message)
        {
            this.errorType = errorType;
            this.source = source;
            this.stackTrace = stackTrace;
        }

        /// <summary>
        /// Creates a new exception from an exception thrown by a handler, using its base exception's type, text, and stack trace.
        /// A relayed <see cref="CqrsServiceException"/> keeps its error type and stack trace, and its source is followed by this source.
        /// </summary>
        /// <param name="ex">The exception thrown by the handler.</param>
        /// <param name="source">The source of the error.</param>
        public CqrsServiceException(Exception ex, string? source)
            : base(ex.GetBaseException().Message)
        {
            var baseException = ex.GetBaseException();
            if (baseException is CqrsServiceException relayed)
            {
                this.errorType = relayed.errorType;
                this.source = $"{relayed.source} - {source}";
                this.stackTrace = relayed.stackTrace;
            }
            else
            {
                this.errorType = baseException.GetType().Name;
                this.source = source;
                this.stackTrace = baseException.StackTrace;
            }
        }

        /// <summary>
        /// Gets the type of error that occurred as thrown on the remote service.
        /// AOT support prevents deserialization of the actual Exception type.
        /// </summary>
        public string ErrorType => this.errorType;

        /// <inheritdoc />
        public override string? Source => this.source;

        /// <inheritdoc />
        public override string? StackTrace => this.stackTrace ?? base.StackTrace;

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{source} - {errorType} - {base.ToString()}";
        }
    }
}