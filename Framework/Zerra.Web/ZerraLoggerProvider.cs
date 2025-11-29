// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.Extensions.Logging;
using Zerra.Collections;

namespace Zerra.Web
{
    public sealed class ZerraLoggerProvider : ILoggerProvider
    {
        private readonly ConcurrentFactoryDictionary<string, ZerraLogger> loggers = new();
        private readonly Zerra.Logging.ILogger log;

        public ZerraLoggerProvider(Zerra.Logging.ILogger log)
        {
            this.log = log;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return loggers.GetOrAdd(categoryName, CreateLoggerImplementation);
        }

        private ZerraLogger CreateLoggerImplementation(string categoryName)
        {
            return new ZerraLogger(log);
        }

        public void Dispose()
        {
            loggers.Clear();
            GC.SuppressFinalize(this);
        }
    }
}
