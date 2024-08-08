// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq;
using Zerra.Serialization.Json;

namespace Zerra.CQRS.Network
{
    public sealed class ApiRequestData
    {
        public string? ProviderType { get; set; }
        public string? ProviderMethod { get; set; }
        public string[]? ProviderArguments { get; set; }

        public string? MessageType { get; set; }
        public string? MessageData { get; set; }
        public bool MessageAwait { get; set; }
        public bool MessageResult { get; set; }

        public string? Source { get; set; }

        public void AddProviderArguments(object[] arguments)
        {
            this.ProviderArguments = arguments.Select(x => JsonSerializer.Serialize(x)).ToArray();
        }
    }
}