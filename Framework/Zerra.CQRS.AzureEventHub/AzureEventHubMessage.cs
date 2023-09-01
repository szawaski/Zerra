// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.CQRS.AzureEventHub
{
    public sealed class AzureEventHubMessage
    {
        public IMessage Message { get; set; }
        public string[][] Claims { get; set; }
        public string Source { get; set; }
    }
}
