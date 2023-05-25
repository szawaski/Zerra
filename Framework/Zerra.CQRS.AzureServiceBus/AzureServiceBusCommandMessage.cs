// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.CQRS.AzureServiceBus
{
    public sealed class AzureServiceBusCommandMessage
    {
        public ICommand Message { get; set; }
        public string[][] Claims { get; set; }
    }
}
