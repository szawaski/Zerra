// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Logging
{
    /// <summary>
    /// Implement this class to provide a mechanism for the framework to log events.
    /// Call the static <see cref="Log"/> class to utilize the implementation.
    /// This follows Microsoft's Logging Levels in Microsoft.Extensions.Logging
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Log an event with a level of Trace.
        /// </summary>
        /// <param name="message">The message to log.</param>
        void Trace(string message);
        /// <summary>
        /// Log an event with a level of Debug.
        /// </summary>
        /// <param name="message">The message to log.</param>
        void Debug(string message);
        /// <summary>
        /// Log an event with a level of Information.
        /// </summary>
        /// <param name="message">The message to log.</param>
        void Info(string message);
        /// <summary>
        /// Log an event with a level of Warning.
        /// </summary>
        /// <param name="message">The message to log.</param>
        void Warn(string message);
        /// <summary>
        /// Log an event with a level of Error.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="ex">The exception to log.</param>
        void Error(string? message = null, Exception? ex = null);
        /// <summary>
        /// Log an event with a level of Error.
        /// </summary>
        /// <param name="ex">The exception to log.</param>
        void Error(Exception? ex = null);
        /// <summary>
        /// Log an event with a level of Critical.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="ex">The exception to log.</param>
        void Critical(string? message = null, Exception? ex = null);
        /// <summary>
        /// Log an event with a level of Critical.
        /// </summary>
        /// <param name="ex">The exception to log.</param>
        void Critical(Exception? ex = null);
    }
}
