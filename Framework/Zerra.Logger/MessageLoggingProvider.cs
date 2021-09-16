// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Threading.Tasks;
using Zerra.CQRS;

namespace Zerra.Logger
{
    public class MessageLoggingProvider : IMessageLogger
    {
        private static readonly string fileName = AppDomain.CurrentDomain.FriendlyName + "-Messages.txt";

        private readonly string file;
        public MessageLoggingProvider()
        {
            var filePath = Config.GetSetting("LogFileDirectory");
            if (!String.IsNullOrWhiteSpace(filePath))
                file = $"{filePath}\\{fileName}";
        }

        public async Task SaveAsync(Type messageType, IMessage message)
        {
            await Task.Run(async () =>
            {
                if (String.IsNullOrWhiteSpace(file))
                    return;

                await LogFile.Log(file, null, messageType.GetNiceName());
            });
        }
    }
}