// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Zerra.Logging;

namespace Zerra.Logger
{
    public sealed class LoggingProvider : ILoggingProvider
    {
        private static readonly string infoFileName = $"{AppDomain.CurrentDomain.FriendlyName}.txt";
        private static readonly string traceFileName = $"{AppDomain.CurrentDomain.FriendlyName}-Verbose.txt";

        private const string traceCategory = "Trace";
        private const string debugCategory = "Debug";
        private const string infoCategory = "Info";
        private const string warnCategory = "Warn";
        private const string errorCategory = "Error";
        private const string criticalCategory = "Critical";

        private readonly string infoFile;
        private readonly string tracefile;

        public LoggingProvider()
        {
            var filePath = Config.GetSetting("LogFileDirectory");
            if (String.IsNullOrWhiteSpace(filePath))
                filePath = System.IO.Path.GetDirectoryName(Environment.CurrentDirectory);

            infoFile = $"{filePath}\\{infoFileName}";
            tracefile = $"{filePath}\\{traceFileName}";
        }

        public async Task TraceAsync(string message)
        {
            var msg = $"{traceCategory}: {message}";
            Debug.WriteLine(msg, traceCategory);
            Console.WriteLine(msg);

            if (!String.IsNullOrWhiteSpace(tracefile))
                await LogFile.Log(tracefile, traceCategory, message);
        }

        public async Task DebugAsync(string message)
        {
            var msg = $"{debugCategory}: {message}";
            Debug.WriteLine(msg, debugCategory);
            Console.WriteLine(msg);

            if (!String.IsNullOrWhiteSpace(tracefile))
                await LogFile.Log(tracefile, debugCategory, message);
        }

        public async Task InfoAsync(string message)
        {
            var msg = $"{infoCategory}: {message}";
            Debug.WriteLine(msg, infoCategory);
            Console.WriteLine(msg);

            if (!String.IsNullOrWhiteSpace(tracefile))
                await LogFile.Log(tracefile, infoCategory, message);
        }

        public async Task WarnAsync(string message)
        {
            var msg = $"{warnCategory}: {message}";
            Debug.WriteLine(msg);
            Console.WriteLine(msg);

            if (!String.IsNullOrWhiteSpace(infoFile))
                await LogFile.Log(infoFile, warnCategory, message);
        }

        public async Task ErrorAsync(string? message, Exception? exception)
        {
            var sb = new StringBuilder();
            _ = sb.Append(errorCategory).Append(": ").Append(message).Append(Environment.NewLine);
            if (exception != null)
            {
                var ex = exception;
                while (ex != null)
                {
                    _ = sb.Append(exception.GetType().Name).Append(": ").Append(exception.Message).Append(Environment.NewLine);
                    _ = sb.Append(exception.StackTrace);

                    ex = ex.InnerException;
                }
            }

            var msg = sb.ToString();

            Debug.WriteLine(msg, errorCategory);
            Console.WriteLine(msg);

            if (!String.IsNullOrWhiteSpace(infoFile))
                await LogFile.Log(infoFile, errorCategory, msg);
        }

        public async Task CriticalAsync(string? message, Exception? exception)
        {
            var sb = new StringBuilder();
            _ = sb.Append(criticalCategory).Append(": ").Append(message).Append(Environment.NewLine);
            if (exception != null)
            {
                var ex = exception;
                while (ex != null)
                {
                    _ = sb.Append(exception.GetType().Name).Append(": ").Append(exception.Message).Append(Environment.NewLine);
                    _ = sb.Append(exception.StackTrace);

                    ex = ex.InnerException;
                }
            }

            var msg = sb.ToString();

            Debug.WriteLine(msg, criticalCategory);
            Console.WriteLine(msg);

            if (!String.IsNullOrWhiteSpace(infoFile))
                await LogFile.Log(infoFile, criticalCategory, msg);
        }
    }
}