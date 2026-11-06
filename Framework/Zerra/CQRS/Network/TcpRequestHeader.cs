// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.CQRS.Network
{
    internal sealed class TcpRequestHeader
    {
        public ReadOnlyMemory<byte> BodyStartBuffer { get; }

        public bool IsError { get; }
        public bool IsUpload { get; }
        public ContentType? ContentType { get; }

        public TcpRequestHeader(ReadOnlyMemory<byte> bodyStartBuffer, bool isError, bool isUpload, ContentType? contentType)
        {
            this.BodyStartBuffer = bodyStartBuffer;
            this.IsError = isError;
            this.IsUpload = isUpload;
            this.ContentType = contentType;
        }
    }
}
