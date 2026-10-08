// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.Extensions.Logging;
using Xunit;
using Zerra.Test.Helpers;
using Zerra.Web;

namespace Zerra.Test.Web
{
    public class ZerraLoggerTests
    {
        [Fact]
        public void Log_MapsLevels()
        {
            var log = new RecordingLogger();
            var logger = new ZerraLogger(log);
            var exception = new InvalidOperationException("boom");

            logger.LogTrace("trace {Value}", 1);
            logger.LogDebug("debug");
            logger.LogInformation("info");
            logger.LogWarning("warn");
            logger.LogError(exception, "error");
            logger.LogCritical(exception, "critical");
            logger.Log(LogLevel.None, "none");

            Assert.Equal(
            [
                ("Trace", "trace 1", null),
                ("Debug", "debug", null),
                ("Info", "info", null),
                ("Warn", "warn", null),
                ("Error", "error", exception),
                ("Critical", "critical", exception)
            ], log.Entries.ToArray());

            Assert.True(logger.IsEnabled(LogLevel.Trace));
            Assert.Null(logger.BeginScope("scope"));
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => logger.Log((LogLevel)99, "bad"));
        }

        [Fact]
        public void Provider_CachesLoggerPerCategory()
        {
            var log = new RecordingLogger();
            using var provider = new ZerraLoggerProvider(log);

            var logger = provider.CreateLogger("A");
            Assert.Same(logger, provider.CreateLogger("A"));
            Assert.NotSame(logger, provider.CreateLogger("B"));

            logger.LogInformation("info");
            Assert.Equal(("Info", "info", null), Assert.Single(log.Entries));
        }

        [Fact]
        public void AddZerraLogger_WritesThroughFactory()
        {
            var log = new RecordingLogger();
            using var factory = new LoggerFactory();
            Assert.Same(factory, factory.AddZerraLogger(log));

            factory.CreateLogger("Test").LogWarning("warn");

            Assert.Equal(("Warn", "warn", null), Assert.Single(log.Entries));
        }
    }
}