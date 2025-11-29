// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.Extensions.Logging;

namespace Zerra.Web
{
    public sealed class ZerraLogger : ILogger
    {
        private readonly Zerra.Logging.ILogger log;
        public ZerraLogger(Zerra.Logging.ILogger log)
        {
            this.log = log;
        }

#if NET6_0
#pragma warning disable CS8633 // Nullability in constraints for type parameter doesn't match the constraints for type parameter in implicitly implemented interface method'.
#pragma warning disable CS8766 // Nullability of reference types in return type doesn't match implicitly implemented member (possibly because of nullability attributes).
#endif
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
#if NET6_0
#pragma warning restore CS8766 // Nullability of reference types in return type doesn't match implicitly implemented member (possibly because of nullability attributes).
#pragma warning restore CS8633 // Nullability in constraints for type parameter doesn't match the constraints for type parameter in implicitly implemented interface method'.
#endif
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

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
