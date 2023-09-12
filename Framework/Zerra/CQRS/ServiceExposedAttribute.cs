// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Zerra.CQRS
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
    public sealed class ServiceExposedAttribute : Attribute
    {
        public NetworkType NetworkType { get; private set; }
        public ServiceExposedAttribute(NetworkType networkType = NetworkType.Api)
        {
            this.NetworkType = networkType;
        }
    }
}
