// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.Extensions.Logging;
using System;
using Zerra.Logging;
using Zerra.Reflection;

namespace Zerra.Web
{
    public sealed class ZerraLogger : ILogger
    {
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
            if (Resolver.TryGetSingle<ILoggingProvider>(out var provider))
            {
                switch (logLevel)
                {
                    case LogLevel.None: break;
                    case LogLevel.Debug: _ = provider.DebugAsync(message); break;
                    case LogLevel.Trace: _ = provider.TraceAsync(message); break;
                    case LogLevel.Information: _ = provider.InfoAsync(message); break;
                    case LogLevel.Warning: _ = provider.WarnAsync(message); break;
                    case LogLevel.Critical: _ = provider.CriticalAsync(message, exception); break;
                    case LogLevel.Error: _ = provider.ErrorAsync(message, exception); break;
                    default: throw new ArgumentOutOfRangeException(nameof(logLevel));
                }
            }
        }
    }
}
