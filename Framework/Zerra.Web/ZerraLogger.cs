// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.Extensions.Logging;
using System;
using Zerra.Logging;
using Zerra.Providers;

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
            string message = formatter(state, null);
            if (Resolver.TryGet(out ILoggingProvider provider))
            {
                switch (logLevel)
                {
                    case LogLevel.None: break;
                    case LogLevel.Debug: provider.DebugAsync(message); break;
                    case LogLevel.Trace: provider.TraceAsync(message); break;
                    case LogLevel.Information: provider.InfoAsync(message); break;
                    case LogLevel.Warning: provider.WarnAsync(message); break;
                    case LogLevel.Critical: provider.CriticalAsync(message, exception); break;
                    case LogLevel.Error: provider.ErrorAsync(message, exception); break;
                    default: throw new ArgumentOutOfRangeException(nameof(logLevel));
                }
            }
        }
    }
}
