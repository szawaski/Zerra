// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Reflection;

namespace Zerra.CQRS
{
    public static partial class Bus
    {
        private readonly struct MethodMetadata
        {
            public MethodDetail MethodDetail { get; }
            public NetworkType BlockedNetworkType { get; }
            public BusLogging BusLogging { get; }
            public bool Authenticate { get; }
            public string[]? Roles { get; }
            public MethodMetadata(MethodDetail methodDetail, NetworkType blockedNetworkType, BusLogging busLogging, bool authenticate, string[]? roles)
            {
                this.MethodDetail = methodDetail;
                this.BlockedNetworkType = blockedNetworkType;
                this.BusLogging = busLogging;
                this.Authenticate = authenticate;
                this.Roles = roles;
            }
        }
    }
}