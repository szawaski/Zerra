// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.CQRS.Network
{
    internal class HostAndPort
    {
        public string Host { get; }
        public int Port { get; }
        public HostAndPort(string host, int port)
        {
            this.Host = host;
            this.Port = port;
        }

        public override bool Equals(object? obj)
        {
            if (obj is not HostAndPort casted)
                return false;
            return casted.Port == this.Port && casted.Host.Equals(this.Host, StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode()
        {
#if NETSTANDARD2_0
            unchecked
            {
                return (int)Math.Pow(Host.GetHashCode(), Port);
            }
#else
            return HashCode.Combine(Host, Port);
#endif
        }
    }
}
