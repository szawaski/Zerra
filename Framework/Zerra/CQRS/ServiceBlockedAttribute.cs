// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.CQRS
{
    [AttributeUsage(AttributeTargets.Method)]
    public class ServiceBlockedAttribute : Attribute
    {
        public ServiceBlockedAttribute()
        {
        }
    }
}
