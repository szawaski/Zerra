// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.IO;

namespace Zerra.CQRS.Network
{
    public class ApiResponseData
    {
        public byte[] Bytes { get; private set; }
        public Stream Stream { get; private set; }
        public bool Void { get { return Bytes == null && Stream == null; } }
        public ApiResponseData()
        {

        }
        public ApiResponseData(byte[] bytes)
        {
            this.Bytes = bytes;
        }
        public ApiResponseData(Stream stream)
        {
            this.Stream = stream;
        }
    }
}