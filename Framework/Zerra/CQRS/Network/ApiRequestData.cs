// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Reflection;

namespace Zerra.CQRS.Network
{
    [GenerateTypeDetail]
    public sealed class ApiRequestData
    {
        public string? ProviderType { get; set; }
        public string? ProviderMethod { get; set; }
        public byte[]?[]? ProviderArguments { get; set; }

        public string? MessageType { get; set; }
        public string? MessageData { get; set; }
        public bool MessageAwait { get; set; }
        public bool MessageResult { get; set; }

        public string? Source { get; set; }
    }
}