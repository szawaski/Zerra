// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.CQRS.Network
{
    public sealed class TcpRequestHeader
    {
        public ReadOnlyMemory<byte> BodyStartBuffer { get; private set; }

        public bool IsError { get; private set; }
        public ContentType? ContentType { get; private set; }
        public string? ProviderType { get; private set; }

        public TcpRequestHeader(ReadOnlyMemory<byte> bodyStartBuffer, bool isError, ContentType? contentType, string? providerType)
        {
            this.BodyStartBuffer = bodyStartBuffer;
            this.IsError = isError;
            this.ContentType = contentType;
            this.ProviderType = providerType;
        }
    }
}