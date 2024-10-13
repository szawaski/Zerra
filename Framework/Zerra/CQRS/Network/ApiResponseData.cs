// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.IO;

namespace Zerra.CQRS.Network
{
    public sealed class ApiResponseData
    {
        public byte[]? Bytes { get; }
        public Stream? Stream { get; }
        public bool Void { get { return Bytes is null && Stream is null; } }
        public ApiResponseData()
        {
            this.Bytes = null;
            this.Stream = null;
        }
        public ApiResponseData(byte[] bytes)
        {
            this.Bytes = bytes;
            this.Stream = null;
        }
        public ApiResponseData(Stream stream)
        {
            this.Bytes = null;
            this.Stream = stream;
        }
    }
}