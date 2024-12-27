// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Threading.Tasks;

namespace Zerra.Logging
{
    /// <summary>
    /// Implement this class to provide a mechanism for the framework to log events.
    /// Call the static <see cref="Log"/> class to utilize the implementation.
    /// This follows Microsoft's Logging Levels in Microsoft.Extensions.Logging
    /// </summary>
    public interface ILoggingProvider
    {
        /// <summary>
        /// Log an event with a level of Trace.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <returns>A task to await completing of the logging.</returns>
        Task TraceAsync(string message);
        /// <summary>
        /// Log an event with a level of Debug.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <returns>A task to await completing of the logging.</returns>
        Task DebugAsync(string message);
        /// <summary>
        /// Log an event with a level of Information.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <returns>A task to await completing of the logging.</returns>
        Task InfoAsync(string message);
        /// <summary>
        /// Log an event with a level of Warning.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <returns>A task to await completing of the logging.</returns>
        Task WarnAsync(string message);
        /// <summary>
        /// Log an event with a level of Error.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="ex">The exception to log.</param>
        /// <returns>A task to await completing of the logging.</returns>
        Task ErrorAsync(string? message, Exception? ex);
        /// <summary>
        /// Log an event with a level of Critical.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="ex">The exception to log.</param>
        /// <returns>A task to await completing of the logging.</returns>
        Task CriticalAsync(string? message, Exception? ex);
    }
}
