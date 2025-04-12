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
    /// <summary>
    /// HttpClient request content for uploading data to a stream.
    /// </summary>
    public sealed class WriteStreamContent : HttpContent
    {
        private readonly Func<Stream, Task>? streamAsyncDelegate;
        private readonly Action<Stream>? streamDelegate;
        /// <summary>
        /// Creates with a delegate for an async upload stream
        /// </summary>
        /// <param name="streamDelegate">The delegate that returns the task to upload to the stream.</param>
        public WriteStreamContent(Func<Stream, Task> streamDelegate)
        {
            if (streamDelegate is null)
                throw new ArgumentNullException(nameof(streamDelegate));
            this.streamAsyncDelegate = streamDelegate;
            this.streamDelegate = null;
        }
        /// <summary>
        /// Creates with a delegate for a synchronous upload stream.
        /// </summary>
        /// <param name="streamDelegate">The delegate that uploads to the stream.</param>
        public WriteStreamContent(Action<Stream> streamDelegate)
        {
            if (streamDelegate is null)
                throw new ArgumentNullException(nameof(streamDelegate));
            this.streamAsyncDelegate = null;
            this.streamDelegate = streamDelegate;
        }

        /// <inheritdoc />
        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            if (streamAsyncDelegate is not null)
                await streamAsyncDelegate(stream);
            else if (streamDelegate is not null)
                streamDelegate(stream);
            else
                throw new InvalidOperationException($"{nameof(WriteStreamContent)} did not initialize correctly");
        }
#if NET5_0_OR_GREATER
        /// <inheritdoc />
        protected override void SerializeToStream(Stream stream, TransportContext? context, CancellationToken cancellationToken)
        {
            if (streamAsyncDelegate is not null)
                Task.Run(() => streamAsyncDelegate(stream)).Wait();
            else if (streamDelegate is not null)
                streamDelegate(stream);
            else
                throw new InvalidOperationException($"{nameof(WriteStreamContent)} did not initialize correctly");
        }

        /// <inheritdoc />
        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken)
        {
            if (streamAsyncDelegate is not null)
                await streamAsyncDelegate(stream);
            else if (streamDelegate is not null)
                streamDelegate(stream);
            else
                throw new InvalidOperationException($"{nameof(WriteStreamContent)} did not initialize correctly");
        }
#endif
        /// <inheritdoc />
        protected override bool TryComputeLength(out long length)
        {
            length = default;
            return false;
        }
    }
}