// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Zerra.CQRS;

namespace Zerra.Logger
{
    public sealed class BusLoggingProvider : IBusLogger
    {
        private static readonly string fileName = AppDomain.CurrentDomain.FriendlyName + "-Bus.txt";

        private const string commandCategory = "Command";
        private const string eventCategory = "Event";
        private const string callCategory = "Call";

        private readonly string file;
        private readonly TelemetryClient telemetryClient;
        public BusLoggingProvider()
        {
            var config = TelemetryConfiguration.CreateDefault();
            telemetryClient = new TelemetryClient(config);

            var filePath = Config.GetSetting("LogFileDirectory");
            if (String.IsNullOrWhiteSpace(filePath))
                filePath = System.IO.Path.GetDirectoryName(Environment.CurrentDirectory);
            file = $"{filePath}\\{fileName}";
        }

        public async Task LogCommandAsync(Type commandType, ICommand command, string source, bool handledInternal, long milliseconds, Exception ex)
        {
            var typeName = commandType.GetNiceName();
            var user = System.Threading.Thread.CurrentPrincipal?.Identity?.Name ?? "[Not Authenticated]";

            string message;
            if (ex != null)
                message = $"{commandCategory} Failed: {typeName} from {source} as {user} error {ex.GetType().Name}:{ex.Message} for {milliseconds}ms";
            else
                message = $"{commandCategory}: {typeName} from {source} as {user} for {milliseconds}ms";

            Debug.WriteLine(message, commandCategory);
            Console.WriteLine(message);
            telemetryClient.TrackTrace(message);

            if (!String.IsNullOrWhiteSpace(file))
                await LogFile.Log(file, commandCategory, message);
        }

        public async Task LogEventAsync(Type eventType, IEvent @event, string source, bool handledInternal, long milliseconds, Exception ex)
        {
            var typeName = eventType.GetNiceName();
            var user = System.Threading.Thread.CurrentPrincipal?.Identity?.Name ?? "[Not Authenticated]";

            string message;
            if (ex != null)
                message = $"{eventCategory} Failed: {typeName} from {source} as {user} error {ex.GetType().Name}:{ex.Message} for {milliseconds}ms";
            else
                message = $"{eventCategory}: {typeName} from {source} as {user} for {milliseconds}ms";

            Debug.WriteLine(message, eventCategory);
            Console.WriteLine(message);
            telemetryClient.TrackTrace(message);

            if (!String.IsNullOrWhiteSpace(file))
                await LogFile.Log(file, eventCategory, message);
        }

        public async Task LogCallAsync(Type interfaceType, string methodName, object[] arguments, string source, bool handledInternal, long milliseconds, Exception ex)
        {
            var interfaceName = interfaceType.GetNiceName();
            var user = System.Threading.Thread.CurrentPrincipal?.Identity?.Name ?? "[Not Authenticated]";

            string message;
            if (ex != null)
                message = $"{callCategory} Failed: {interfaceName}.{methodName} from {source} as {user} error {ex.GetType().Name}:{ex.Message} for {milliseconds}ms";
            else
                message = $"{callCategory}: {interfaceName}.{methodName} from {source} as {user} for {milliseconds}ms";

            Debug.WriteLine(message, callCategory);
            Console.WriteLine(message);
            telemetryClient.TrackTrace(message);

            if (!String.IsNullOrWhiteSpace(file))
                await LogFile.Log(file, callCategory, message);
        }
    }
}