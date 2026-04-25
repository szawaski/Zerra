// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.CQRS.Network
{
    internal sealed class HttpRequestHeader
    {
        public ReadOnlyMemory<byte> BodyStartBuffer { get; set; }

        public string? Declarations { get; set; }
        public Dictionary<string, List<string?>>? Headers { get; set; }
        public bool IsError { get; set; }
        public ContentType? ContentType { get; set; }
        public int? ContentLength { get; set; }
        public bool Chuncked { get; set; }

        public string? ProviderType { get; set; }
        public string? Origin { get; set; }
        public bool Preflight { get; set; }

        public bool? RelayServiceAddRemove { get; set; }
        public string? RelayKey { get; set; }
    }
}