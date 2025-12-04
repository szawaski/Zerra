// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.CQRS
{
    internal sealed class HandlerMetadata
    {
        public BusLogging BusLogging { get; }
        public HandlerMetadata(BusLogging busLogging)
        {
            this.BusLogging = busLogging;
        }
    }
}
