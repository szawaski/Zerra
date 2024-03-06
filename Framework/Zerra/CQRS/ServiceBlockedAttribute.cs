// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.CQRS
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ServiceBlockedAttribute : Attribute
    {
        public NetworkType NetworkType { get; }
        public ServiceBlockedAttribute(NetworkType networkType)
        {
            this.NetworkType = networkType;
        }
    }
}
