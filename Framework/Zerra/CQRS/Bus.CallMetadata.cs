// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Collections;

namespace Zerra.CQRS
{
    public static partial class Bus
    {
        private readonly struct CallMetadata
        {
            public NetworkType ExposedNetworkType { get; }
            public BusLogging BusLogging { get; }
            public bool Authenticate { get; }
            public string[]? Roles { get; }
            public ConcurrentFactoryDictionary<string, MethodMetadata> MethodMetadata { get; }
            public CallMetadata(NetworkType exposedNetworkType, BusLogging busLogging, bool authenticate, string[]? roles)
            {
                this.ExposedNetworkType = exposedNetworkType;
                this.BusLogging = busLogging;
                this.Authenticate = authenticate;
                this.Roles = roles;
                this.MethodMetadata = new();
            }
        }
    }
}