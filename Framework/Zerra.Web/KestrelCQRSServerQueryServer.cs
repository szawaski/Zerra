// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Threading;
using Zerra.CQRS;

namespace Zerra.Web
{
    public sealed class KestrelCqrsServerQueryServer : IQueryServer
    {
        private readonly KestrelCqrsServerLinkedSettings settings;
        public KestrelCqrsServerQueryServer(KestrelCqrsServerLinkedSettings settings)
        {
            this.settings = settings;
        }

        public void Close() { }

        public void Open() { }

        public void RegisterInterfaceType(int maxConcurrent, Type type)
        {
            lock (settings.Types)
            {
                if (settings.Types.ContainsKey(type))
                    return;
                var throttle = new SemaphoreSlim(maxConcurrent, maxConcurrent);
                if (!settings.Types.TryAdd(type, throttle))
                    throttle.Dispose();
            }
        }

        void IQueryServer.Setup(CommandCounter commandCounter, QueryHandlerDelegate providerHandlerAsync)
        {
            settings.CommandCounter = commandCounter;
            settings.ProviderHandlerAsync = providerHandlerAsync;
        }
    }
}