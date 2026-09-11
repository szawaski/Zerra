// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.CQRS.Network
{
    //a struct so a lookup per request doesn't allocate, IEquatable so dictionary lookups don't box
    internal readonly struct HostAndPort : IEquatable<HostAndPort>
    {
        public string Host { get; }
        public int Port { get; }
        public HostAndPort(string host, int port)
        {
            this.Host = host;
            this.Port = port;
        }

        public bool Equals(HostAndPort other) => other.Port == this.Port && String.Equals(other.Host, this.Host, StringComparison.OrdinalIgnoreCase);

        public override bool Equals(object? obj) => obj is HostAndPort casted && Equals(casted);

        public override int GetHashCode()
        {
            //case-insensitive to match Equals
#if NETSTANDARD2_0
            unchecked
            {
                return (StringComparer.OrdinalIgnoreCase.GetHashCode(Host) * 397) ^ Port;
            }
#else
            return HashCode.Combine(StringComparer.OrdinalIgnoreCase.GetHashCode(Host), Port);
#endif
        }
    }
}
