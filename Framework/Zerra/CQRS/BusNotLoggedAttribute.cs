// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.CQRS
{
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method)]
    public sealed class BusLoggedAttribute : Attribute
    {
        public BusLogging BusLogging { get; private set; }
        public BusLoggedAttribute(BusLogging busLogging)
        {
            BusLogging = busLogging;
        }
    }
}
