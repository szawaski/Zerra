// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Logging
{
    /// <summary>
    /// Support for legacy version
    /// </summary>
    [Obsolete("Use Zerra.Logging.ILog interface with dependency injection instead")]
    public static class Log
    {
        private static ILog log;
        public static void SetLog(ILog log)
        {
            Log.log = log;
        }

        /// <summary>
        /// Log an event with a level of Trace.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <returns>A task to await completing of the logging.</returns>
        public static Task TraceAsync(string message)
        {
            log.Info(message);
            return Task.CompletedTask;
        }
        /// <summary>
        /// Log an event with a level of Debug.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <returns>A task to await completing of the logging.</returns>
        public static Task DebugAsync(string message)
        {
            log.Debug(message);
            return Task.CompletedTask;
        }
        /// <summary>
        /// Log an event with a level of Trace.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <returns>A task to await completing of the logging.</returns>
        public static Task InfoAsync(string message)
        {
            log.Info(message);
            return Task.CompletedTask;
        }
        /// <summary>
        /// Log an event with a level of Warning.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <returns>A task to await completing of the logging.</returns>
        public static Task WarnAsync(string message)
        {
            log.Warn(message);
            return Task.CompletedTask;
        }
        /// <summary>
        /// Log an event with a level of Error.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="exception">The exception to log.</param>
        /// <returns>A task to await completing of the logging.</returns>
        public static Task ErrorAsync(string message, Exception? exception = null)
        {
            log.Error(message, exception);
            return Task.CompletedTask;
        }
        /// <summary>
        /// Log an event with a level of Error.
        /// </summary>
        /// <param name="exception">The exception to log.</param>
        /// <returns>A task to await completing of the logging.</returns>
        public static Task ErrorAsync(Exception exception)
        {
            log.Error(exception);
            return Task.CompletedTask;
        }
        /// <summary>
        /// Log an event with a level of Critical.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="exception">The exception to log.</param>
        /// <returns>A task to await completing of the logging.</returns>
        public static Task CriticalAsync(string message, Exception? exception = null)
        {
            log.Critical(message, exception);
            return Task.CompletedTask;
        }
        /// <summary>
        /// Log an event with a level of Critical.
        /// </summary>
        /// <param name="exception">The exception to log.</param>
        /// <returns>A task to await completing of the logging.</returns>
        public static Task CriticalAsync(Exception exception)
        {
            log.Critical(exception);
            return Task.CompletedTask;
        }
    }
}
