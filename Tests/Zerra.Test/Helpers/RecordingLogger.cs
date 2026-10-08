// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections.Concurrent;

namespace Zerra.Test.Helpers
{
    public sealed class RecordingLogger : Zerra.Logging.ILogger
    {
        public ConcurrentQueue<(string Level, string? Message, Exception? Exception)> Entries { get; } = new();

        public void Trace(string message) => Entries.Enqueue(("Trace", message, null));
        public void Debug(string message) => Entries.Enqueue(("Debug", message, null));
        public void Info(string message) => Entries.Enqueue(("Info", message, null));
        public void Warn(string message) => Entries.Enqueue(("Warn", message, null));
        public void Error(string? message = null, Exception? ex = null) => Entries.Enqueue(("Error", message, ex));
        public void Error(Exception? ex = null) => Entries.Enqueue(("Error", null, ex));
        public void Critical(string? message = null, Exception? ex = null) => Entries.Enqueue(("Critical", message, ex));
        public void Critical(Exception? ex = null) => Entries.Enqueue(("Critical", null, ex));
    }
}