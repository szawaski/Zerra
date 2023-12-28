// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;

namespace Zerra.CQRS
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Method)]
    public sealed class ServiceSecureAttribute : Attribute
    {
        public IReadOnlyCollection<string>? Roles { get; private set; }

        public ServiceSecureAttribute()
        {
            Roles = null;
        }

        public ServiceSecureAttribute(params string[] roles)
        {
            if (roles.Length == 0)
                this.Roles = null;
            else
                this.Roles = roles;
        }
        public ServiceSecureAttribute(IReadOnlyCollection<string> roles)
        {
            if (roles.Count == 0)
                this.Roles = null;
            else
                this.Roles = roles;
        }
    }
}
