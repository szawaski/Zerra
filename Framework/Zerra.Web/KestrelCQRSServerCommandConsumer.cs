// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Threading;
using Zerra.CQRS;

namespace Zerra.Web
{
    public sealed class KestrelCQRSServerCommandConsumer : ICommandConsumer
    {
        private readonly KestrelCQRSServerLinkedSettings settings;
        public KestrelCQRSServerCommandConsumer(KestrelCQRSServerLinkedSettings settings)
        {
            this.settings = settings;
        }

        public string ServiceUrl => throw new NotImplementedException();

        public void Close() { }

        public IEnumerable<Type> GetCommandTypes()
        {
            return settings.CommandTypes.Keys;
        }

        public void Open() { }

        void ICommandConsumer.Setup(CommandCounter commandCounter, HandleRemoteCommandDispatch handlerAsync, HandleRemoteCommandDispatch handlerAwaitAsync)
        {
            settings.ReceiveCounter = commandCounter;
            settings.HandlerAsync = handlerAsync;
            settings.HandlerAwaitAsync = handlerAwaitAsync;
        }

        void ICommandConsumer.RegisterCommandType(int maxConcurrent, string topic, Type type)
        {
            if (settings.CommandTypes.ContainsKey(type))
                return;
            var throttle = new SemaphoreSlim(maxConcurrent, maxConcurrent);
            if (!settings.CommandTypes.TryAdd(type, throttle))
                throttle.Dispose();
        }
    }
}