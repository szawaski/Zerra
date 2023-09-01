// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.IO;

namespace Zerra.CQRS
{
    public sealed class RemoteQueryCallResponse
    {
        private readonly object model;
        private readonly Stream stream;

        public object Model => model;
        public Stream Stream => stream;

        public RemoteQueryCallResponse(object model)
        {
            this.model = model;
            this.stream = null;
        }
        public RemoteQueryCallResponse(Stream stream)
        {
            this.model = null;
            this.stream = stream;
        }
    }
}