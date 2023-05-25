// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.IO;

namespace Zerra.CQRS
{
    public sealed class RemoteQueryCallResponse
    {
        public object Model { get; private set; }
        public Stream Stream { get; private set; }
        public RemoteQueryCallResponse(object model)
        {
            this.Model = model;
        }
        public RemoteQueryCallResponse(Stream stream)
        {
            this.Stream = stream;
        }
    }
}