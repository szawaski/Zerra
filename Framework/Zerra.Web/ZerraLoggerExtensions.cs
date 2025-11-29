// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.Extensions.Logging;

namespace Zerra.Web
{
    public static class ZerraLoggerExtensions
    {
        public static ILoggerFactory AddZerraLogger(this ILoggerFactory factory, Zerra.Logging.ILogger log)
        {
            factory.AddProvider(new ZerraLoggerProvider(log));
            return factory;
        }
    }
}
