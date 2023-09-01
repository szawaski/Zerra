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


        public async ValueTask LogCommandAsync(Type commandType, ICommand command, string source)
        {
            var typeName = commandType.GetNiceName();
            var user = System.Threading.Thread.CurrentPrincipal?.Identity?.Name ?? "[Not Authenticated]";

            var message = $"{commandCategory}: {typeName} from {source} as {user}";
            Debug.WriteLine(message, commandCategory);
            Console.WriteLine(message);
            telemetryClient.TrackTrace(message);

            if (!String.IsNullOrWhiteSpace(file))
                await LogFile.Log(file, commandCategory, message);
        }

        public async ValueTask LogEventAsync(Type eventType, IEvent @event, string source)
        {
            var typeName = eventType.GetNiceName();
            var user = System.Threading.Thread.CurrentPrincipal?.Identity?.Name ?? "[Not Authenticated]";

            var message = $"{eventCategory}: {typeName} from {source} as {user}";
            Debug.WriteLine(message, eventCategory);
            Console.WriteLine(message);
            telemetryClient.TrackTrace(message);

            if (!String.IsNullOrWhiteSpace(file))
                await LogFile.Log(file, eventCategory, message);
        }

        public async ValueTask LogCallAsync(Type interfaceType, string methodName, object[] arguments, string source)
        {
            var interfaceName = interfaceType.GetNiceName();
            var user = System.Threading.Thread.CurrentPrincipal?.Identity?.Name ?? "[Not Authenticated]";

            var message = $"{callCategory}: {interfaceName}.{methodName} from {source} as {user}";
            Debug.WriteLine(message, callCategory);
            Console.WriteLine(message);
            telemetryClient.TrackTrace(message);

            if (!String.IsNullOrWhiteSpace(file))
                await LogFile.Log(file, callCategory, message);
        }
    }
}