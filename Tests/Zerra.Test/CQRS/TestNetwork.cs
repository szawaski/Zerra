// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Net;
using System.Net.Sockets;

namespace Zerra.Test.CQRS
{
    internal static class TestNetwork
    {
        //a port the OS reports free on IPv4 and IPv6, localhost resolves to both, so tests running at the same time never share one
        public static string NewUrl()
        {
            using var socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
            socket.DualMode = true;
            socket.Bind(new IPEndPoint(IPAddress.IPv6Any, 0));
            return $"http://localhost:{((IPEndPoint)socket.LocalEndPoint!).Port}";
        }
    }
}
