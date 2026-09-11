// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Net;
using System.Net.Sockets;
using Xunit;
using Zerra.CQRS.Network;

namespace Zerra.Test.CQRS.Network
{
    public class IPResolverTests
    {
        [Fact]
        public void GetIPEndPoints_AnyHost_ListensOnAllAddresses()
        {
            var endpoints = IPResolver.GetIPEndPoints(["*:9001"]);

            Assert.Equal([new IPEndPoint(IPAddress.Any, 9001), new IPEndPoint(IPAddress.IPv6Any, 9001)], endpoints);
        }

        [Fact]
        public void GetIPEndPoints_IPAddress_ResolvesToIt()
        {
            var endpoints = IPResolver.GetIPEndPoints(["http://127.0.0.1:9001"]);

            Assert.Equal([new IPEndPoint(IPAddress.Loopback, 9001)], endpoints);
        }

        [Fact]
        public void GetIPEndPoints_HostDoesNotResolve_Throws()
        {
            //a server must not start without anything to listen on
            _ = Assert.ThrowsAny<SocketException>(() => IPResolver.GetIPEndPoints(["invalid.host.that.does.not.exist.example:9001"]));
        }
    }
}
