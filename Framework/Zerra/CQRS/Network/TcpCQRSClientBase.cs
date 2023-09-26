// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Linq;
using System.Net;
using Zerra.Logging;

namespace Zerra.CQRS.Network
{
    public abstract class TcpCQRSClientBase : CQRSClientBase
    {
        protected readonly IPEndPoint ipEndpoint;

        public TcpCQRSClientBase(string serviceUrl) : base(serviceUrl)
        {
            var endpoints = IPResolver.GetIPEndPoints(serviceUrl);
            if (endpoints.Count > 1)
                _ = Log.WarnAsync($"Client resolved more than endpoint at {serviceUrl} as {String.Join(", ", endpoints.Select(x => x.ToString()))}");
            this.ipEndpoint = endpoints.First();
        }
    }
}