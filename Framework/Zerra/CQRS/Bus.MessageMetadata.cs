// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.CQRS
{
    public static partial class Bus
    {
        private readonly struct MessageMetadata
        {
            public NetworkType ExposedNetworkType { get; }
            public BusLogging BusLogging { get; }
            public bool Authenticate { get; }
            public string[]? Roles { get; }
            public MessageMetadata(NetworkType exposedNetworkType, BusLogging busLogging, bool authenticate, string[]? roles)
            {
                this.ExposedNetworkType = exposedNetworkType;
                this.BusLogging = busLogging;
                this.Authenticate = authenticate;
                this.Roles = roles;
            }
        }
    }
}