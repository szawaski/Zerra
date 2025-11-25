// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using System.Linq;
using Zerra.CQRS.Network;

namespace Zerra.Test
{
    public class NetworkingTest
    {
        [Fact]
        public void ResolveEndpoint()
        {
            var endpoints = IPResolver.GetIPEndPoints("https://google.com").ToArray();
        }
    }
}
