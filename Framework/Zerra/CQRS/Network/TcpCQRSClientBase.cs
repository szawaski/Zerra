// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq;
using System.Net;

namespace Zerra.CQRS.Network
{
    public abstract class TcpCQRSClientBase : CQRSClientBase
    {
        protected readonly IPEndPoint endpoint;

        public TcpCQRSClientBase(string serviceUrl) : base(serviceUrl)
        {
            var endpoints = IPResolver.GetIPEndPoints(serviceUrl);
            //if (endpoints.Count > 1)
            //    throw new Exception($"Client cannot have more than endpoint {nameof(serviceUrl)} {serviceUrl} found {String.Join(", ", endpoints.Select(x => x.ToString()))}");
            this.endpoint = endpoints.First();
        }
    }
}