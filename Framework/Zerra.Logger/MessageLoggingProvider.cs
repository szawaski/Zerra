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
    public sealed class MessageLoggingProvider : IMessageLogger
    {
        private static readonly string fileName = AppDomain.CurrentDomain.FriendlyName + "-Messages.txt";

        private const string category = "Messages";

        private readonly string file;
        private readonly TelemetryClient telemetryClient;
        public MessageLoggingProvider()
        {
            var config = TelemetryConfiguration.CreateDefault();
            telemetryClient = new TelemetryClient(config);

            var filePath = Config.GetSetting("LogFileDirectory");
            if (String.IsNullOrWhiteSpace(filePath))
                filePath = System.IO.Path.GetDirectoryName(Environment.CurrentDirectory);
            file = $"{filePath}\\{fileName}";
        }

        public async Task SaveAsync(Type messageType, IMessage message)
        {
            var messageTypeName = messageType.GetNiceName();

            await Task.Run(() =>
            {
                var msg = $"{category}: {messageTypeName}";
                Debug.WriteLine(msg, category);
                Console.WriteLine(msg);
                telemetryClient.TrackEvent(messageTypeName);
            });

            if (!String.IsNullOrWhiteSpace(file))
                await LogFile.Log(file, null, messageTypeName);
        }
    }
}