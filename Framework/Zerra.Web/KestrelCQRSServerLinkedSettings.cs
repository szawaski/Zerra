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
        //each value can be a full origin such as https://app.example.com or just the host such as app.example.com, compared without case
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
            this.allowOriginsString = "*";
        }

        public void Dispose()
        {
            //the throttles are not disposed: requests still running after Dispose release them, and a disposed SemaphoreSlim throws ObjectDisposedException on Release
            //this isn't a leak, SemaphoreSlim.Dispose only frees the wait handle that AvailableWaitHandle creates on first use, which nothing reads,
            //the rest is managed memory with no finalizer that the GC reclaims once nothing references them
            //if AvailableWaitHandle is ever used, dispose them once every request that could release them has finished
            Types.Clear();
        }
    }
}