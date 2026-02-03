// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.Extensions.Logging;

namespace Zerra.Web
{
    /// <summary>
    /// Extension methods for integrating Zerra logging with Microsoft.Extensions.Logging.
    /// </summary>
    public static class ZerraLoggerExtensions
    {
        /// <summary>
        /// Adds Zerra logger provider to the logger factory.
        /// </summary>
        /// <remarks>
        /// Configures the logger factory to use Zerra's logging implementation for all loggers.
        /// This allows ASP.NET Core components to log through Zerra's centralized logging system.
        /// </remarks>
        /// <param name="factory">The logger factory to add the provider to.</param>
        /// <param name="log">The underlying Zerra logger implementation.</param>
        /// <returns>The logger factory for method chaining.</returns>
        public static ILoggerFactory AddZerraLogger(this ILoggerFactory factory, Zerra.Logging.ILogger log)
        {
            factory.AddProvider(new ZerraLoggerProvider(log));
            return factory;
        }
    }
}
