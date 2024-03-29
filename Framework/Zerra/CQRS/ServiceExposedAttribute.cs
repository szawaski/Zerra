﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.CQRS
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
    public sealed class ServiceExposedAttribute : Attribute
    {
        public NetworkType NetworkType { get; }
        public ServiceExposedAttribute(NetworkType networkType = NetworkType.Api)
        {
            this.NetworkType = networkType;
        }
    }
}
