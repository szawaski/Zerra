// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Concurrent;
using System.Threading;
using Zerra.CQRS;
using Zerra.CQRS.Network;

namespace Zerra.Web
{
    public sealed class KestrelCQRSServerLinkedSettings : IDisposable
    {
        public ConcurrentDictionary<Type, SemaphoreSlim> InterfaceTypes { get; private set; }
        public ConcurrentDictionary<Type, SemaphoreSlim> CommandTypes { get; private set; }

        public CommandCounter? ReceiveCounter { get; set; }

        public QueryHandlerDelegate? ProviderHandlerAsync { get; set; }
        public HandleRemoteCommandDispatch? HandlerAsync { get; set; }
        public HandleRemoteCommandDispatch? HandlerAwaitAsync { get; set; }

        public string? Route { get; private set; }
        public ICqrsAuthorizer? Authorizer { get; private set; }
        public ContentType ContentType { get; private set; }

        private string[]? allowOrigins;
        public string[]? AllowOrigins
        {
            get
            {
                return allowOrigins;
            }
            set
            {
                allowOrigins = value == null || value.Length == 0 ? null : value;
                allowOriginsString = value == null || value.Length == 0 ? "*" : String.Join(", ", value);
            }
        }

        private string allowOriginsString;
        public string AllowOriginsString
        {
            get
            {
                return allowOriginsString;
            }
        }

        public KestrelCQRSServerLinkedSettings(string? route, ICqrsAuthorizer? authorizer, ContentType contentType)
        {
            this.Route = route;
            this.Authorizer = authorizer;
            this.ContentType = contentType;

            InterfaceTypes = new();
            CommandTypes = new();
            this.allowOriginsString = "*";
        }

        public void Dispose()
        {
            foreach (var throttle in InterfaceTypes.Values)
                throttle.Dispose();
            InterfaceTypes.Clear();

            foreach (var throttle in CommandTypes.Values)
                throttle.Dispose();
            CommandTypes.Clear();
        }
    }
}