// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
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
            return settings.InterfaceTypes;
        }

        public void Open() { }

        public void RegisterInterfaceType(Type type)
        {
            settings.InterfaceTypes.Add(type);
        }

        public void SetHandler(QueryHandlerDelegate providerHandlerAsync)
        {
            settings.ProviderHandlerAsync = providerHandlerAsync;
        }
    }
}