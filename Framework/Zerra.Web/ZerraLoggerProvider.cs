// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.Extensions.Logging;
using Zerra.Collections;

namespace Zerra.Web
{
    /// <summary>
    /// Logger provider that creates Zerra loggers for use with Microsoft.Extensions.Logging.
    /// </summary>
    /// <remarks>
    /// Integrates Zerra's logging implementation with ASP.NET Core's dependency injection logging system.
    /// Creates and caches logger instances per category name.
    /// </remarks>
    public sealed class ZerraLoggerProvider : ILoggerProvider
    {
        private readonly ConcurrentFactoryDictionary<string, ZerraLogger> loggers = new();
        private readonly Zerra.Logging.ILogger log;

        /// <summary>
        /// Initializes a new instance of the <see cref="ZerraLoggerProvider"/> class.
        /// </summary>
        /// <param name="log">The underlying Zerra logger implementation to use for all created loggers.</param>
        public ZerraLoggerProvider(Zerra.Logging.ILogger log)
        {
            this.log = log;
        }

        /// <summary>
        /// Creates a logger for the specified category name.
        /// </summary>
        /// <remarks>
        /// Loggers are cached by category name, so repeated calls with the same category return the same instance.
        /// </remarks>
        /// <param name="categoryName">The category name for the logger (typically the type name of the logging component).</param>
        /// <returns>A logger instance for the specified category.</returns>
        public ILogger CreateLogger(string categoryName)
        {
            return loggers.GetOrAdd(categoryName, CreateLoggerImplementation);
        }

        /// <summary>
        /// Creates a new logger implementation for a given category.
        /// </summary>
        /// <param name="categoryName">The category name (unused, as all loggers delegate to the same underlying logger).</param>
        /// <returns>A new logger instance.</returns>
        private ZerraLogger CreateLoggerImplementation(string categoryName)
        {
            return new ZerraLogger(log);
        }

        /// <summary>
        /// Releases all resources used by the <see cref="ZerraLoggerProvider"/>.
        /// </summary>
        /// <remarks>
        /// Clears the logger cache. After disposal, the provider cannot be used to create new loggers.
        /// </remarks>
        public void Dispose()
        {
            loggers.Clear();
            GC.SuppressFinalize(this);
        }
    }
}
