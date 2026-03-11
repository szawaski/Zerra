// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Logging
{
    /// <summary>
    /// Support for legacy version
    /// </summary>
    //[Obsolete("Use Zerra.Logging.ILogger interface with dependency injection instead")]
    public static class Log
    {
        private static ILogger? instance;
        /// <summary>
        /// Sets the logging implementation to be used by the application.
        /// </summary>
        /// <remarks>Call this method to configure the logging behavior before any logging operations are
        /// performed. Subsequent log messages will be handled by the specified <paramref name="log"/>
        /// instance.</remarks>
        /// <param name="log">The logging instance that provides log message handling. Cannot be null.</param>
        public static void SetLog(ILogger log)
        {
            Log.instance = log;
        }

        /// <summary>
        /// Log an event with a level of Trace.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public static void Trace(string message)
        {
            instance?.Info(message);
        }
        /// <summary>
        /// Log an event with a level of Debug.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public static void Debug(string message)
        {
            instance?.Debug(message);
        }
        /// <summary>
        /// Log an event with a level of Trace.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public static void Info(string message)
        {
            instance?.Info(message);
        }
        /// <summary>
        /// Log an event with a level of Warning.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public static void Warn(string message)
        {
            instance?.Warn(message);
        }
        /// <summary>
        /// Log an event with a level of Error.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="exception">The exception to log.</param>
        public static void Error(string message, Exception? exception = null)
        {
            instance?.Error(message, exception);
        }
        /// <summary>
        /// Log an event with a level of Error.
        /// </summary>
        /// <param name="exception">The exception to log.</param>
        public static void Error(Exception exception)
        {
            instance?.Error(exception);
        }
        /// <summary>
        /// Log an event with a level of Critical.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="exception">The exception to log.</param>
        public static void Critical(string message, Exception? exception = null)
        {
            instance?.Critical(message, exception);
        }

        /// <summary>
        /// Log an event with a level of Critical.
        /// </summary>
        /// <param name="exception">The exception to log.</param>
        public static void Critical(Exception exception)
        {
            instance?.Critical(exception);
        }

        /// <summary>
        /// Log an event with a level of Trace.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <returns>A task to await completing of the logging.</returns>
        [Obsolete("Use non-async version")]
        public static Task TraceAsync(string message)
        {
            instance?.Info(message);
            return Task.CompletedTask;
        }
        /// <summary>
        /// Log an event with a level of Debug.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <returns>A task to await completing of the logging.</returns>
        [Obsolete("Use non-async version")]
        public static Task DebugAsync(string message)
        {
            instance?.Debug(message);
            return Task.CompletedTask;
        }
        /// <summary>
        /// Log an event with a level of Trace.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <returns>A task to await completing of the logging.</returns>
        [Obsolete("Use non-async version")]
        public static Task InfoAsync(string message)
        {
            instance?.Info(message);
            return Task.CompletedTask;
        }
        /// <summary>
        /// Log an event with a level of Warning.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <returns>A task to await completing of the logging.</returns>
        [Obsolete("Use non-async version")]
        public static Task WarnAsync(string message)
        {
            instance?.Warn(message);
            return Task.CompletedTask;
        }
        /// <summary>
        /// Log an event with a level of Error.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="exception">The exception to log.</param>
        /// <returns>A task to await completing of the logging.</returns>
        [Obsolete("Use non-async version")]
        public static Task ErrorAsync(string message, Exception? exception = null)
        {
            instance?.Error(message, exception);
            return Task.CompletedTask;
        }
        /// <summary>
        /// Log an event with a level of Error.
        /// </summary>
        /// <param name="exception">The exception to log.</param>
        /// <returns>A task to await completing of the logging.</returns>
        [Obsolete("Use non-async version")]
        public static Task ErrorAsync(Exception exception)
        {
            instance?.Error(exception);
            return Task.CompletedTask;
        }
        /// <summary>
        /// Log an event with a level of Critical.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="exception">The exception to log.</param>
        /// <returns>A task to await completing of the logging.</returns>
        [Obsolete("Use non-async version")]
        public static Task CriticalAsync(string message, Exception? exception = null)
        {
            instance?.Critical(message, exception);
            return Task.CompletedTask;
        }
        /// <summary>
        /// Log an event with a level of Critical.
        /// </summary>
        /// <param name="exception">The exception to log.</param>
        /// <returns>A task to await completing of the logging.</returns>
        [Obsolete("Use non-async version")]
        public static Task CriticalAsync(Exception exception)
        {
            instance?.Critical(exception);
            return Task.CompletedTask;
        }
    }
}
