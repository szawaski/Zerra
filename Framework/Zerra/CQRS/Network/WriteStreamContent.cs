// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.IO;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net;
using System.Threading;

namespace Zerra.CQRS.Network
{
    public sealed class WriteStreamContent : HttpContent
    {
        private readonly Func<Stream, Task> streamAsyncDelegate;
        private readonly Action<Stream> streamDelegate;
        public WriteStreamContent(Func<Stream, Task> streamDelegate)
        {
            if (streamDelegate == null)
                throw new ArgumentNullException(nameof(streamDelegate));
            this.streamAsyncDelegate = streamDelegate;
            this.streamDelegate = null;
        }
        public WriteStreamContent(Action<Stream> streamDelegate)
        {
            if (streamDelegate == null)
                throw new ArgumentNullException(nameof(streamDelegate));
            this.streamAsyncDelegate = null;
            this.streamDelegate = streamDelegate;
        }

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            if (streamAsyncDelegate != null)
                await streamAsyncDelegate(stream);
            else
                streamDelegate(stream);
        }
#if NET5_0_OR_GREATER
        protected override void SerializeToStream(Stream stream, TransportContext context, CancellationToken cancellationToken)
        {
            if (streamAsyncDelegate != null)
                streamAsyncDelegate(stream).GetAwaiter().GetResult();
            else
                streamDelegate(stream);
        }

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context, CancellationToken cancellationToken)
        {
            if (streamAsyncDelegate != null)
                await streamAsyncDelegate(stream);
            else
                streamDelegate(stream);
        }
#endif
        protected override bool TryComputeLength(out long length)
        {
            length = default;
            return false;
        }
    }
}