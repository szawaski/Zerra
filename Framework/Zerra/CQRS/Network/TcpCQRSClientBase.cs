// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Linq;
using System.Net;
using Zerra.Logging;

namespace Zerra.CQRS.Network
{
    public abstract class TcpCqrsClientBase : CqrsClientBase
    {
        protected readonly object ipEndpointLock = new();
        private IPEndPoint ipEndpoint = null;

        protected IPEndPoint IpEndPoint
        {
            get
            {
                if (ipEndpoint == null)
                {
                    lock (ipEndpointLock)
                    {
                        if (ipEndpoint == null)
                        {
                            var endpoints = IPResolver.GetIPEndPoints(serviceUrl.OriginalString);
                            if (endpoints.Count == 0)
                                throw new Exception($"Client could not resolve endpoint at {serviceUrl}");
                            if (endpoints.Count > 1)
                                _ = Log.WarnAsync($"Client resolved more than endpoint at {serviceUrl} as {String.Join(", ", endpoints.Select(x => x.ToString()))}");
                            ipEndpoint = endpoints.First();
                        }
                    }
                }
                return ipEndpoint;
            }
        }

        public TcpCqrsClientBase(string serviceUrl) : base(serviceUrl)
        {

        }
    }
}