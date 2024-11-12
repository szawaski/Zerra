// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections.Generic;

namespace Zerra.CQRS
{
    public static partial class Bus
    {
        private class MethodMetadata
        {
            public NetworkType BlockedNetworkType { get; }
            public BusLogging BusLogging { get; }
            public bool Authenticate { get; }
            public IReadOnlyCollection<string>? Roles { get; }
            public MethodMetadata(NetworkType blockedNetworkType, BusLogging busLogging, bool authenticate, IReadOnlyCollection<string>? roles)
            {
                this.BlockedNetworkType = blockedNetworkType;
                this.BusLogging = busLogging;
                this.Authenticate = authenticate;
                this.Roles = roles;
            }
        }
    }
}