// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
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
        private readonly TelemetryClient telemetryClient;
        public LoggingProvider()
        {
            var config = TelemetryConfiguration.CreateDefault();
            telemetryClient = new TelemetryClient(config);

            var filePath = Config.GetSetting("LogFileDirectory");
            if (String.IsNullOrWhiteSpace(filePath))
                filePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

            infoFile = $"{filePath}\\{infoFileName}";
            tracefile = $"{filePath}\\{traceFileName}";
        }

        public async Task TraceAsync(string message)
        {
            await Task.Run(() =>
            {
                var msg = $"{traceCategory}: {message}";
                Debug.WriteLine(msg, traceCategory);
                Console.WriteLine(msg);
                telemetryClient.TrackTrace(message);
            });

            if (!String.IsNullOrWhiteSpace(tracefile))
                await LogFile.Log(tracefile, traceCategory, message);
        }

        public async Task DebugAsync(string message)
        {
            await Task.Run(() =>
            {
                var msg = $"{debugCategory}: {message}";
                Debug.WriteLine(msg, debugCategory);
                Console.WriteLine(msg);
                telemetryClient.TrackTrace(message);
            });

            if (!String.IsNullOrWhiteSpace(tracefile))
                await LogFile.Log(tracefile, debugCategory, message);
        }

        public async Task InfoAsync(string message)
        {
            await Task.Run(() =>
            {
                var msg = $"{infoCategory}: {message}";
                Debug.WriteLine(msg, infoCategory);
                Console.WriteLine(msg);
                telemetryClient.TrackEvent(message);
            });

            if (!String.IsNullOrWhiteSpace(tracefile))
                await LogFile.Log(tracefile, infoCategory, message);
        }

        public async Task WarnAsync(string message)
        {
            await Task.Run(() =>
            {
                var msg = $"{warnCategory}: {message}";
                Debug.WriteLine(msg);
                Console.WriteLine(msg);
                telemetryClient.TrackEvent(message);
            });

            if (!String.IsNullOrWhiteSpace(infoFile))
                await LogFile.Log(infoFile, warnCategory, message);
        }

        public async Task ErrorAsync(string message, Exception exception)
        {
            var sb = new StringBuilder();
            sb.Append(errorCategory).Append(": ").Append(message).Append(Environment.NewLine);
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

            var msg = sb.ToString();

            await Task.Run(() =>
            {
                Debug.WriteLine(msg, errorCategory);
                Console.WriteLine(msg);
                if (exception != null)
                    telemetryClient.TrackException(exception);
                else
                    telemetryClient.TrackEvent(msg);
            });

            if (!String.IsNullOrWhiteSpace(infoFile))
                await LogFile.Log(infoFile, errorCategory, msg);
        }

        public async Task CriticalAsync(string message, Exception exception)
        {
            var sb = new StringBuilder();
            sb.Append(criticalCategory).Append(": ").Append(message).Append(Environment.NewLine);
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

            var msg = sb.ToString();

            await Task.Run(() =>
            {
                Debug.WriteLine(msg, criticalCategory);
                Console.WriteLine(msg);
                if (exception != null)
                    telemetryClient.TrackException(exception);
                else
                    telemetryClient.TrackEvent(msg);
            });

            if (!String.IsNullOrWhiteSpace(infoFile))
                await LogFile.Log(infoFile, criticalCategory, msg);
        }
    }
}