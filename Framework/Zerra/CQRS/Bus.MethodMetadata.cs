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
            public bool Blocked { get; }
            public BusLogging BusLogging { get; }
            public bool Authenticate { get; }
            public IReadOnlyCollection<string>? Roles { get; }
            public MethodMetadata(bool blocked, BusLogging busLogging, bool authenticate, IReadOnlyCollection<string>? roles)
            {
                this.Blocked = blocked;
                this.BusLogging = busLogging;
                this.Authenticate = authenticate;
                this.Roles = roles;
            }
        }
    }
}