// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.Extensions.Logging;
using System;
using Zerra.Collections;

namespace Zerra.Web
{
    public class ZerraLoggerProvider : ILoggerProvider
    {
        private readonly ConcurrentFactoryDictionary<string, ZerraLogger> loggers = new();

        public ILogger CreateLogger(string categoryName)
        {
            return loggers.GetOrAdd(categoryName, CreateLoggerImplementation);
        }

        private ZerraLogger CreateLoggerImplementation(string categoryName)
        {
            return new ZerraLogger();
        }

        public void Dispose()
        {
            loggers.Clear();
            GC.SuppressFinalize(this);
        }
    }
}
