// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Threading;
using Zerra.CQRS;

namespace Zerra.Web
{
    public sealed class KestrelCqrsServerCommandConsumer : ICommandConsumer
    {
        private readonly KestrelCqrsServerLinkedSettings settings;
        public KestrelCqrsServerCommandConsumer(KestrelCqrsServerLinkedSettings settings)
        {
            this.settings = settings;
        }

        public void Close() { }

        public void Open() { }

        void ICommandConsumer.Setup(CommandCounter commandCounter, HandleRemoteCommandDispatch handlerAsync, HandleRemoteCommandDispatch handlerAwaitAsync, HandleRemoteCommandWithResultDispatch handlerWithResultAwaitAsync)
        {
            settings.CommandCounter = commandCounter;
            settings.CommandHandlerAsync = handlerAsync;
            settings.CommandHandlerAwaitAsync = handlerAwaitAsync;
            settings.CommandHandlerWithResultAwaitAsync = handlerWithResultAwaitAsync;
        }

        void ICommandConsumer.RegisterCommandType(int maxConcurrent, string topic, Type type)
        {
            if (settings.Types.ContainsKey(type))
                return;
            var throttle = new SemaphoreSlim(maxConcurrent, maxConcurrent);
            if (!settings.Types.TryAdd(type, throttle))
                throttle.Dispose();
        }
    }
}