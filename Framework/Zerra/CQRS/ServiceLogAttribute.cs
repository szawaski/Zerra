// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.CQRS
{
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method)]
    public sealed class ServiceLogAttribute : Attribute
    {
        public BusLogging BusLogging { get; private set; }
        public ServiceLogAttribute(BusLogging busLogging)
        {
            BusLogging = busLogging;
        }
    }
}
