// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using Zerra.CQRS.Network;

namespace Zerra.Test.CQRS.Network
{
    public class HostAndPortTests
    {
        [Fact]
        public void Equals_SameHostAndPort_IsEqual()
        {
            var a = new HostAndPort("example.com", 9001);
            var b = new HostAndPort("example.com", 9001);

            Assert.True(a.Equals(b));
            Assert.True(a.Equals((object)b));
            Assert.Equal(a.GetHashCode(), b.GetHashCode());
        }

        //host names are case-insensitive, the hash has to agree with Equals or a dictionary lookup misses
        [Fact]
        public void Equals_HostDiffersOnlyByCase_IsEqualWithSameHash()
        {
            var a = new HostAndPort("Example.COM", 9001);
            var b = new HostAndPort("example.com", 9001);

            Assert.True(a.Equals(b));
            Assert.Equal(a.GetHashCode(), b.GetHashCode());
        }

        [Fact]
        public void Equals_DifferentPort_IsNotEqual()
        {
            Assert.False(new HostAndPort("example.com", 9001).Equals(new HostAndPort("example.com", 9002)));
        }

        [Fact]
        public void Equals_DifferentHost_IsNotEqual()
        {
            Assert.False(new HostAndPort("example.com", 9001).Equals(new HostAndPort("example.org", 9001)));
        }

        [Fact]
        public void Equals_OtherType_IsNotEqual()
        {
            Assert.False(new HostAndPort("example.com", 9001).Equals("example.com:9001"));
            Assert.False(new HostAndPort("example.com", 9001).Equals(null));
        }

        //the socket pool keys its pools and throttles by host and port
        [Fact]
        public void DictionaryKey_FindsEntryRegardlessOfHostCase()
        {
            var dictionary = new Dictionary<HostAndPort, int>() { [new HostAndPort("Example.com", 9001)] = 1 };

            Assert.True(dictionary.TryGetValue(new HostAndPort("EXAMPLE.com", 9001), out var value));
            Assert.Equal(1, value);
            Assert.False(dictionary.ContainsKey(new HostAndPort("example.com", 9002)));
        }
    }
}
