// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using Zerra.Logging;
using Zerra.Test.Helpers;

namespace Zerra.Test.Logging
{
    public class LogTests
    {
        [Fact]
        public async Task Log_WritesToSetLogger()
        {
            var log = new RecordingLogger();
            Log.SetLog(log);
            try
            {
                var exception = new InvalidOperationException("boom");

                Log.Trace("trace");
                Log.Debug("debug");
                Log.Info("info");
                Log.Warn("warn");
                Log.Error("error", exception);
                Log.Error(exception);
                Log.Critical("critical", exception);
                Log.Critical(exception);
#pragma warning disable CS0618 // Type or member is obsolete
                await Log.TraceAsync("trace");
                await Log.DebugAsync("debug");
                await Log.InfoAsync("info");
                await Log.WarnAsync("warn");
                await Log.ErrorAsync("error", exception);
                await Log.ErrorAsync(exception);
                await Log.CriticalAsync("critical", exception);
                await Log.CriticalAsync(exception);
#pragma warning restore CS0618 // Type or member is obsolete

                (string, string?, Exception?)[] expected =
                [
                    ("Trace", "trace", null),
                    ("Debug", "debug", null),
                    ("Info", "info", null),
                    ("Warn", "warn", null),
                    ("Error", "error", exception),
                    ("Error", null, exception),
                    ("Critical", "critical", exception),
                    ("Critical", null, exception)
                ];
                Assert.Equal([.. expected, .. expected], log.Entries.ToArray());
            }
            finally
            {
                Log.SetLog(null!);
            }
        }

        [Fact]
        public void Log_WithoutLogger_DoesNothing()
        {
            Log.SetLog(null!);
            Log.Info("info");
            Log.Error(new InvalidOperationException());
        }
    }
}