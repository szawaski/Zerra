// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.CQRS
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
    public class ServiceExposedAttribute : Attribute
    {
        public ServiceExposedAttribute()
        {
        }
    }
}
