// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.CQRS.AzureServiceBus
{
    public sealed class AzureServiceBusEventMessage
    {
        public IEvent Message { get; set; }
        public string[][] Claims { get; set; }
        public string Source { get; set; }
    }
}
