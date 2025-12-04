// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.Extensions.Logging;

namespace Zerra.Web
{
    /// <summary>
    /// Adapter that bridges Microsoft.Extensions.Logging.ILogger to Zerra's logging interface.
    /// </summary>
    /// <remarks>
    /// Converts ASP.NET Core logging calls to Zerra's internal logging implementation.
    /// Allows Zerra components to work seamlessly within ASP.NET Core dependency injection and logging infrastructure.
    /// </remarks>
    public sealed class ZerraLogger : ILogger
    {
        private readonly Zerra.Logging.ILog log;

        /// <summary>
        /// Initializes a new instance of the <see cref="ZerraLogger"/> class.
        /// </summary>
        /// <param name="log">The underlying Zerra logger implementation.</param>
        public ZerraLogger(Zerra.Logging.ILog log)
        {
            this.log = log;
        }

#if NET6_0
#pragma warning disable CS8633 // Nullability in constraints for type parameter doesn't match the constraints for type parameter in implicitly implemented interface method'.
#pragma warning disable CS8766 // Nullability of reference types in return type doesn't match implicitly implemented member (possibly because of nullability attributes).
#endif
        /// <summary>
        /// Begins a logical scope for the logger.
        /// </summary>
        /// <remarks>
        /// This implementation returns null as Zerra does not support logging scopes.
        /// </remarks>
        /// <typeparam name="TState">The state type.</typeparam>
        /// <param name="state">The scope state.</param>
        /// <returns>Always returns null.</returns>
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
#if NET6_0
#pragma warning restore CS8766 // Nullability of reference types in return type doesn't match implicitly implemented member (possibly because of nullability attributes).
#pragma warning restore CS8633 // Nullability in constraints for type parameter doesn't match the constraints for type parameter in implicitly implemented interface method'.
#endif
        {
            return null;
        }

        /// <summary>
        /// Determines whether logging is enabled for the specified log level.
        /// </summary>
        /// <param name="logLevel">The log level to check.</param>
        /// <returns>Always returns true, indicating all log levels are enabled.</returns>
        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        /// <summary>
        /// Writes a log entry to the underlying Zerra logger.
        /// </summary>
        /// <remarks>
        /// Maps Microsoft.Extensions.Logging.LogLevel values to corresponding Zerra logging methods.
        /// </remarks>
        /// <typeparam name="TState">The state type.</typeparam>
        /// <param name="logLevel">The severity level of the log entry.</param>
        /// <param name="eventId">The event identifier (unused).</param>
        /// <param name="state">The state object to log.</param>
        /// <param name="exception">Optional exception associated with the log entry.</param>
        /// <param name="formatter">Delegate to format the state and exception into a message string.</param>
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var message = formatter(state, null);

            switch (logLevel)
            {
                case LogLevel.None: break;
                case LogLevel.Debug: log.Debug(message); break;
                case LogLevel.Trace: log.Trace(message); break;
                case LogLevel.Information: log.Info(message); break;
                case LogLevel.Warning: log.Warn(message); break;
                case LogLevel.Critical: log.Critical(message, exception); break;
                case LogLevel.Error: log.Error(message, exception); break;
                default: throw new ArgumentOutOfRangeException(nameof(logLevel));
            }
        }
    }
}
