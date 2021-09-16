// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.CQRS.Network
{
    public class TcpRequestHeader
    {
        public ReadOnlyMemory<byte> BodyStartBuffer { get; set; }

        public bool IsError { get; set; }
        public ContentType? ContentType { get; set; }
        public string ProviderType { get; set; }
    }
}