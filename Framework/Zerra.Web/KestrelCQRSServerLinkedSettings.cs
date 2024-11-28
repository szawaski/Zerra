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
    public sealed class KestrelCqrsServerLinkedSettings : IDisposable
    {
        public ConcurrentDictionary<Type, SemaphoreSlim> Types { get; }

        public CommandCounter? CommandCounter { get; set; }

        public QueryHandlerDelegate? ProviderHandlerAsync { get; set; }
        public HandleRemoteCommandDispatch? CommandHandlerAsync { get; set; }
        public HandleRemoteCommandDispatch? CommandHandlerAwaitAsync { get; set; }
        public HandleRemoteCommandWithResultDispatch? CommandHandlerWithResultAwaitAsync { get; set; }
        public HandleRemoteEventDispatch? EventHandlerAsync { get; set; }

        public string? Route { get; }
        public ICqrsAuthorizer? Authorizer { get; }
        public ContentType ContentType { get; }

        private string[]? allowOrigins;
        public string[]? AllowOrigins
        {
            get
            {
                return allowOrigins;
            }
            set
            {
                allowOrigins = value is null || value.Length == 0 ? null : value;
                allowOriginsString = value is null || value.Length == 0 ? "*" : String.Join(", ", value);
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

        public KestrelCqrsServerLinkedSettings(string? route, ICqrsAuthorizer? authorizer, ContentType contentType)
        {
            this.Route = route;
            this.Authorizer = authorizer;
            this.ContentType = contentType;

            Types = new();
            Types = new();
            this.allowOriginsString = "*";
        }

        public void Dispose()
        {
            foreach (var throttle in Types.Values)
                throttle.Dispose();
            Types.Clear();

            foreach (var throttle in Types.Values)
                throttle.Dispose();
            Types.Clear();
        }
    }
}