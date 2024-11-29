// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Threading;
using Zerra.CQRS;

namespace Zerra.Web
{
    public sealed class KestrelCqrsServerEventConsumer : IEventConsumer
    {
        private readonly KestrelCqrsServerLinkedSettings settings;
        public KestrelCqrsServerEventConsumer(KestrelCqrsServerLinkedSettings settings)
        {
            this.settings = settings;
        }

        public void Close() { }

        public void Open() { }

        string IEventConsumer.MessageHost => "[Kestrel Host]";

        void IEventConsumer.Setup(HandleRemoteEventDispatch handlerAsync)
        {
            settings.EventHandlerAsync = handlerAsync;
        }

        void IEventConsumer.RegisterEventType(int maxConcurrent, string topic, Type type)
        {
            if (settings.Types.ContainsKey(type))
                return;
            var throttle = new SemaphoreSlim(maxConcurrent, maxConcurrent);
            if (!settings.Types.TryAdd(type, throttle))
                throttle.Dispose();
        }
    }
}