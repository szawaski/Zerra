// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.CQRS.AzureEventHub
{
    public sealed class AzureEventHubMessage
    {
        public byte[]? MessageData { get; set; }
        public Type? MessageType { get; set; }
        public string[][]? Claims { get; set; }
        public string? Source { get; set; }
    }
}
