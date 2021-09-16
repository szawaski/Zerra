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
    public class LoggingProvider : ILoggingProvider
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
            if (!String.IsNullOrWhiteSpace(filePath))
            {
                infoFile = $"{filePath}\\{infoFileName}";
                tracefile = $"{filePath}\\{traceFileName}";
            }
        }

        public async Task TraceAsync(string message)
        {
            await Task.Run(async () =>
            {
                Debug.WriteLine($"{traceCategory}: {message}", traceCategory);

                Console.WriteLine(message);

                if (!String.IsNullOrWhiteSpace(tracefile))
                    await LogFile.Log(tracefile, traceCategory, message);
            });
        }

        public async Task DebugAsync(string message)
        {
            await Task.Run(async () =>
            {
                Debug.WriteLine($"{debugCategory}: {message}", debugCategory);

                Console.WriteLine(message);

                if (!String.IsNullOrWhiteSpace(tracefile))
                    await LogFile.Log(tracefile, debugCategory, message);
            });
        }

        public async Task InfoAsync(string message)
        {
            await Task.Run(async () =>
            {
                Debug.WriteLine($"{infoCategory}: {message}", infoCategory);

                Console.WriteLine(message);

                if (!String.IsNullOrWhiteSpace(tracefile))
                    await LogFile.Log(tracefile, infoCategory, message);
            });
        }

        public async Task WarnAsync(string message)
        {
            await Task.Run(async () =>
            {
                Debug.WriteLine($"{warnCategory}: {message}", warnCategory);

                Console.WriteLine(message);

                if (!String.IsNullOrWhiteSpace(infoFile))
                    await LogFile.Log(infoFile, warnCategory, message);
            });
        }

        public async Task ErrorAsync(string message, Exception exception)
        {
            await Task.Run(async () =>
            {
                Debug.WriteLine($"{errorCategory}: {message}", errorCategory);

                var sb = new StringBuilder();

                if (exception != null)
                {
                    Exception ex = exception;
                    while (ex != null)
                    {
                        sb.Append(exception.GetType().Name).Append(": ").Append(exception.Message).Append(Environment.NewLine);
                        sb.Append(exception.StackTrace);

                        ex = ex.InnerException;
                    }
                }

                var builtMessage = sb.ToString();

                Console.WriteLine(builtMessage);

                if (!String.IsNullOrWhiteSpace(infoFile))
                    await LogFile.Log(infoFile, errorCategory, builtMessage);
            });
        }

        public async Task CriticalAsync(string message, Exception exception)
        {
            await Task.Run(async () =>
            {
                Debug.WriteLine($"{criticalCategory}: {message}", criticalCategory);

                var sb = new StringBuilder();

                if (!String.IsNullOrWhiteSpace(message))
                    sb.Append(message).Append(Environment.NewLine);

                if (exception != null)
                {
                    Exception ex = exception;
                    while (ex != null)
                    {
                        sb.Append(exception.GetType().Name).Append(": ").Append(exception.Message).Append(Environment.NewLine);
                        sb.Append(exception.StackTrace);

                        ex = ex.InnerException;
                    }
                }

                var builtMessage = sb.ToString();

                Console.WriteLine(builtMessage);

                if (!String.IsNullOrWhiteSpace(infoFile))
                    await LogFile.Log(infoFile, criticalCategory, builtMessage);
            });
        }
    }
}