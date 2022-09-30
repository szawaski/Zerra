// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using Zerra.CQRS.Network;

namespace Zerra.Test
{
    [TestClass]
    public class NetworkingTest
    {
        [TestMethod]
        public void ResolveEndpoint()
        {
            var endpoints = IPResolver.GetIPEndPoints("https://qa01-integrations-sync.arcoro.com").ToArray();
        }
    }
}
