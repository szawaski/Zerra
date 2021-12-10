// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.CQRS
{
    [AttributeUsage(AttributeTargets.Method)]
    public class ServiceBlockedAttribute : Attribute
    {
        public NetworkType? NetworkType { get; private set; }
        public ServiceBlockedAttribute()
        {
        }
        public ServiceBlockedAttribute(NetworkType networkType)
        {
            this.NetworkType = networkType;
        }
    }
}
