// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Threading;
using Zerra.Serialization.Json;

namespace Zerra.CQRS.Network
{
    public sealed class ApiRequestData
    {
        public string? ProviderType { get; set; }
        public string? ProviderMethod { get; set; }
        public string?[]? ProviderArguments { get; set; }

        public string? MessageType { get; set; }
        public string? MessageData { get; set; }
        public bool MessageAwait { get; set; }
        public bool MessageResult { get; set; }

        public string? Source { get; set; }

        public void AddProviderArguments(object[] arguments)
        {
            //a trailing CancellationToken isn't sent, the server passes its own in its place
            var serializeCount = arguments.Length > 0 && arguments[arguments.Length - 1] is CancellationToken ? arguments.Length - 1 : arguments.Length;
            var providerArguments = new string?[arguments.Length];
            for (var i = 0; i < serializeCount; i++)
                providerArguments[i] = JsonSerializer.Serialize(arguments[i]);
            this.ProviderArguments = providerArguments;
        }
    }
}