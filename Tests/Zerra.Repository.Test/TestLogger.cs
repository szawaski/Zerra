// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using Zerra.Logging;

namespace Zerra.Repository.Test
{
    /// <summary>
    /// Writes what the messaging transports log to the test output, their listening threads catch and log errors rather than throw.
    /// </summary>
    public sealed class TestLogger : ILogger
    {
        public void Trace(string message) { }
        public void Debug(string message) { }
        public void Info(string message) => Write("INFO", message, null);
        public void Warn(string message) => Write("WARN", message, null);
        public void Error(string? message = null, Exception? ex = null) => Write("ERROR", message, ex);
        public void Error(Exception? ex = null) => Write("ERROR", null, ex);
        public void Critical(string? message = null, Exception? ex = null) => Write("CRITICAL", message, ex);
        public void Critical(Exception? ex = null) => Write("CRITICAL", null, ex);

        private static void Write(string level, string? message, Exception? ex)
        {
            try
            {
                //background threads can still log after the test finished, when there is no output to write to
                TestContext.Current.TestOutputHelper?.WriteLine($"{DateTime.Now:HH:mm:ss.fff} {level} {message} {ex}");
            }
            catch (InvalidOperationException)
            {
            }
        }
    }
}
