﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.CQRS.AzureEventHub
{
    public class AzureEventHubEventMessage
    {
        public IEvent Message { get; set; }
        public string[][] Claims { get; set; }
    }
}