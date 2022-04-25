// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.CQRS.AzureEventHub
{
    public class AzureEventHubCommandMessage
    {
        public ICommand Message { get; set; }
        public string[][] Claims { get; set; }
    }
}
