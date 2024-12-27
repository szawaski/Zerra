// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Threading.Tasks;
using Zerra.Reflection;

namespace Zerra.Logging
{
    /// <summary>
    /// A framework logger that uses an implementation of <see cref="ILoggingProvider"/>.
    /// Discovery will find the implementation.
    /// This follows Microsoft's Logging Levels in Microsoft.Extensions.Logging
    /// </summary>
    public static class Log
    {
        /// <summary>
        /// Log an event with a level of Trace.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <returns>A task to await completing of the logging.</returns>
        public static Task TraceAsync(string message)
        {
            if (Resolver.TryGetSingle<ILoggingProvider>(out var provider))
            {
                return provider.TraceAsync(message);
            }
            return Task.CompletedTask;
        }
        /// <summary>
        /// Log an event with a level of Debug.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <returns>A task to await completing of the logging.</returns>
        public static Task DebugAsync(string message)
        {
            if (Resolver.TryGetSingle<ILoggingProvider>(out var provider))
            {
                return provider.DebugAsync(message);
            }
            return Task.CompletedTask;
        }
        /// <summary>
        /// Log an event with a level of Trace.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <returns>A task to await completing of the logging.</returns>
        public static Task InfoAsync(string message)
        {
            if (Resolver.TryGetSingle<ILoggingProvider>(out var provider))
            {
                return provider.InfoAsync(message);
            }
            return Task.CompletedTask;
        }
        /// <summary>
        /// Log an event with a level of Warning.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <returns>A task to await completing of the logging.</returns>
        public static Task WarnAsync(string message)
        {
            if (Resolver.TryGetSingle<ILoggingProvider>(out var provider))
            {
                return provider.WarnAsync(message);
            }
            return Task.CompletedTask;
        }
        /// <summary>
        /// Log an event with a level of Error.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="ex">The exception to log.</param>
        /// <returns>A task to await completing of the logging.</returns>
        public static Task ErrorAsync(string message, Exception? exception = null)
        {
            if (Resolver.TryGetSingle<ILoggingProvider>(out var provider))
            {
                return provider.ErrorAsync(message, exception);
            }
            return Task.CompletedTask;
        }
        /// <summary>
        /// Log an event with a level of Error.
        /// </summary>
        /// <param name="ex">The exception to log.</param>
        /// <returns>A task to await completing of the logging.</returns>
        public static Task ErrorAsync(Exception exception)
        {
            if (Resolver.TryGetSingle<ILoggingProvider>(out var provider))
            {
                return provider.ErrorAsync(null, exception);
            }
            return Task.CompletedTask;
        }
        /// <summary>
        /// Log an event with a level of Critical.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="ex">The exception to log.</param>
        /// <returns>A task to await completing of the logging.</returns>
        public static Task CriticalAsync(string message, Exception? exception = null)
        {
            if (Resolver.TryGetSingle<ILoggingProvider>(out var provider))
            {
                return provider.CriticalAsync(message, exception);
            }
            return Task.CompletedTask;
        }
        /// <summary>
        /// Log an event with a level of Critical.
        /// </summary>
        /// <param name="ex">The exception to log.</param>
        /// <returns>A task to await completing of the logging.</returns>
        public static Task CriticalAsync(Exception exception)
        {
            if (Resolver.TryGetSingle<ILoggingProvider>(out var provider))
            {
                return provider.CriticalAsync(null, exception);
            }
            return Task.CompletedTask;
        }
    }
}
