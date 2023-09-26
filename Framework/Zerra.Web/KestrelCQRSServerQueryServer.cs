// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Threading;
using Zerra.CQRS;

namespace Zerra.Web
{
    public sealed class KestrelCQRSServerQueryServer : IQueryServer
    {
        private readonly KestrelCQRSServerLinkedSettings settings;
        public KestrelCQRSServerQueryServer(KestrelCQRSServerLinkedSettings settings)
        {
            this.settings = settings;
        }

        public string ServiceUrl => throw new NotImplementedException();

        public void Close() { }

        public ICollection<Type> GetInterfaceTypes()
        {
            return settings.InterfaceTypes.Keys;
        }

        public void Open() { }

        public void RegisterInterfaceType(int maxConcurrent, Type type)
        {
            lock (settings.InterfaceTypes)
            {
                if (settings.InterfaceTypes.ContainsKey(type))
                    return;
                var throttle = new SemaphoreSlim(maxConcurrent, maxConcurrent);
                if (!settings.InterfaceTypes.TryAdd(type, throttle))
                    throttle.Dispose();
            }
        }

        void IQueryServer.Setup(int? maxReceived, Action processExit, QueryHandlerDelegate providerHandlerAsync)
        {
            settings.MaxReceived = maxReceived;
            settings.ProcessExit = processExit;
            settings.ProviderHandlerAsync = providerHandlerAsync;
        }

    }
}