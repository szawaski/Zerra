// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.CQRS.AzureEventHub
{
    public class AzureEventHubMessage
    {
        public string EnvironmentName { get; set; }
        public IMessage Message { get; set; }
        public string[][] Claims { get; set; }
    }
}
