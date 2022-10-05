// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.Extensions.Logging;
using System;
using Zerra.Logging;
using Zerra.Providers;
using Zerra.Reflection;

namespace Zerra.Web
{
    public class ZerraLogger : ILogger
    {
        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var message = formatter(state, null);
            if (Resolver.TryGet(out ILoggingProvider provider))
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
