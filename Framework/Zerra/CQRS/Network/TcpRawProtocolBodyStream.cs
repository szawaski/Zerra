// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Zerra.IO;

namespace Zerra.CQRS.Network
{
    public sealed class TcpRawProtocolBodyStream : StreamTransform
    {
        private static readonly byte[] endingBytes = BitConverter.GetBytes(0);

        private readonly ReadOnlyMemory<byte> readStartBuffer;
        private int readStartBufferPosition;
        private long position;
        private int segmentPosition;
        private int segmentLength;
        private bool ended;
        private const int segmentLengthBufferLength = 4;
        private byte[]? segmentLengthBufferSource;
        private Memory<byte> segmentLengthBuffer;

        public TcpRawProtocolBodyStream(Stream stream, ReadOnlyMemory<byte> readStartBufferPosition, bool leaveOpen) : base(stream, leaveOpen)
        {
            this.readStartBuffer = readStartBufferPosition;
            this.readStartBufferPosition = 0;
            this.position = 0;
            this.segmentPosition = 0;
            this.segmentLength = -1;
            this.ended = false;
            this.segmentLengthBufferSource = BufferArrayPool<byte>.Rent(segmentLengthBufferLength);
            this.segmentLengthBuffer = segmentLengthBufferSource;
        }

        protected override void Dispose(bool disposing)
        {
            if (segmentLengthBufferSource != null)
            {
                BufferArrayPool<byte>.Return(segmentLengthBufferSource);
                segmentLengthBufferSource = null;
                segmentLengthBuffer = null;
            }
            base.Dispose(disposing);
        }
#if !NETSTANDARD2_0
        public override ValueTask DisposeAsync()
        {
            if (segmentLengthBufferSource != null)
            {
                BufferArrayPool<byte>.Return(segmentLengthBufferSource);
                segmentLengthBufferSource = null;
                segmentLengthBuffer = null;
            }
            return base.DisposeAsync();
        }
#endif

        public override long Length => throw new NotSupportedException();
        public override long Position { get { return position; } set { throw new NotSupportedException(); } }

        public override long Seek(long offset, SeekOrigin origin) { throw new NotSupportedException(); }
        public override void SetLength(long value) { throw new NotSupportedException(); }

        protected override int InternalRead(Span<byte> buffer)
        {
            var totalBytesRead = 0;
            int bytesToRead;
            while (totalBytesRead < buffer.Length)
            {
                int bytesRead;
                if (segmentLength == -1)
                {
                    bytesRead = 0;
                    while (bytesRead < segmentLengthBufferLength)
                    {
                        if (readStartBufferPosition < readStartBuffer.Length)
                        {
                            bytesToRead = Math.Min(readStartBuffer.Length - readStartBufferPosition, segmentLengthBufferLength - bytesRead);
                            readStartBuffer.Slice(readStartBufferPosition, bytesToRead).CopyTo(segmentLengthBuffer.Slice(bytesRead, bytesToRead));
                            readStartBufferPosition += bytesToRead;
                            bytesRead += bytesToRead;
                        }
                        else
                        {
#if NETSTANDARD2_0
                            bytesRead += stream.Read(segmentLengthBufferSource, bytesRead, segmentLengthBufferLength - bytesRead);
#else
                            bytesRead += stream.Read(segmentLengthBuffer.Span[bytesRead..segmentLengthBufferLength]);
#endif
                        }

                        if (bytesRead == 0)
                            throw new ConnectionAbortedException();
                    }
#if NETSTANDARD2_0
                    segmentLength = BitConverter.ToInt32(segmentLengthBufferSource, 0);
#else
                    segmentLength = BitConverter.ToInt32(segmentLengthBuffer.Span);
#endif
                    if (segmentLength < 0)
                        throw new CqrsNetworkException("Bad Data");
                }

                if (segmentLength == 0)
                    break;

                bytesToRead = Math.Min(buffer.Length - totalBytesRead, segmentLength - segmentPosition);

                if (readStartBufferPosition < readStartBuffer.Length)
                {
                    bytesToRead = Math.Min(readStartBuffer.Length - readStartBufferPosition, bytesToRead);
                    readStartBuffer.Span.Slice(readStartBufferPosition, bytesToRead).CopyTo(buffer.Slice(totalBytesRead, bytesToRead));
                    readStartBufferPosition += bytesToRead;
                    bytesRead = bytesToRead;
                }
                else
                {
#if NETSTANDARD2_0
                    bytesRead = stream.ReadToSpan(buffer.Slice(totalBytesRead, bytesToRead));
#else
                    bytesRead = stream.Read(buffer.Slice(totalBytesRead, bytesToRead));
#endif
                }
                segmentPosition += bytesRead;
                totalBytesRead += bytesRead;

                if (segmentPosition == segmentLength)
                {
                    segmentPosition = 0;
                    segmentLength = -1;
                }
            }
            position += totalBytesRead;
            return totalBytesRead;
        }
        protected override async ValueTask<int> InternalReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            var totalBytesRead = 0;
            int bytesToRead;
            while (totalBytesRead < buffer.Length)
            {
                int bytesRead;
                if (segmentLength == -1)
                {
                    bytesRead = 0;
                    while (bytesRead < segmentLengthBufferLength)
                    {
                        if (readStartBufferPosition < readStartBuffer.Length)
                        {
                            bytesToRead = Math.Min(readStartBuffer.Length - readStartBufferPosition, segmentLengthBufferLength - bytesRead);
                            readStartBuffer.Slice(readStartBufferPosition, bytesToRead).CopyTo(segmentLengthBuffer.Slice(bytesRead, bytesToRead));
                            readStartBufferPosition += bytesToRead;
                            bytesRead += bytesToRead;
                        }
                        else
                        {
#if NETSTANDARD2_0
                            bytesRead += await stream.ReadAsync(segmentLengthBufferSource, bytesRead, segmentLengthBufferLength - bytesRead);
#else
                            bytesRead += await stream.ReadAsync(segmentLengthBuffer[bytesRead..segmentLengthBufferLength], cancellationToken);
#endif
                        }

                        if (bytesRead == 0)
                            throw new ConnectionAbortedException();
                    }
#if NETSTANDARD2_0
                    segmentLength = BitConverter.ToInt32(segmentLengthBufferSource, 0);
#else
                    segmentLength = BitConverter.ToInt32(segmentLengthBuffer.Span);
#endif
                    if (segmentLength < 0)
                        throw new CqrsNetworkException("Bad Data");
                }

                if (segmentLength == 0)
                    break;

                bytesToRead = Math.Min(buffer.Length - totalBytesRead, segmentLength - segmentPosition);

                if (readStartBufferPosition < readStartBuffer.Length)
                {
                    bytesToRead = Math.Min(readStartBuffer.Length - readStartBufferPosition, bytesToRead);
                    readStartBuffer.Slice(readStartBufferPosition, bytesToRead).CopyTo(buffer.Slice(totalBytesRead, bytesToRead));
                    readStartBufferPosition += bytesToRead;
                    bytesRead = bytesToRead;
                }
                else
                {
#if NETSTANDARD2_0
                    bytesRead = await stream.ReadToMemoryAsync(buffer.Slice(totalBytesRead, bytesToRead), cancellationToken);
#else
                    bytesRead = await stream.ReadAsync(buffer.Slice(totalBytesRead, bytesToRead), cancellationToken);
#endif
                }
                if (bytesRead == 0)
                    break;

                segmentPosition += bytesRead;
                totalBytesRead += bytesRead;

                if (segmentPosition == segmentLength)
                {
                    segmentPosition = 0;
                    segmentLength = -1;
                }
            }

            position += totalBytesRead;
            return totalBytesRead;
        }

        protected override void InternalWrite(ReadOnlySpan<byte> buffer)
        {
            var segmentLengthBytes = BitConverter.GetBytes(buffer.Length);
#if NETSTANDARD2_0
            stream.Write(segmentLengthBytes, 0, segmentLengthBytes.Length);
            stream.Write(buffer.ToArray(), 0, buffer.Length);
#else
            stream.Write(segmentLengthBytes.AsSpan());
            stream.Write(buffer);
#endif
            position += buffer.Length;
        }

        protected override async ValueTask InternalWriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
        {
            var segmentLengthBytes = BitConverter.GetBytes(buffer.Length);

#if NETSTANDARD2_0
            await stream.WriteAsync(segmentLengthBytes, 0, segmentLengthBytes.Length, cancellationToken);
            await stream.WriteAsync(buffer.ToArray(), 0, buffer.Length, cancellationToken);
#else
            await stream.WriteAsync(segmentLengthBytes.AsMemory(), cancellationToken);
            await stream.WriteAsync(buffer, cancellationToken);
#endif
            position += buffer.Length;
        }

        public override void Flush()
        {
            if (ended)
                throw new CqrsNetworkException("body already ended");
            ended = true;
#if NETSTANDARD2_0
            stream.Write(endingBytes, 0, endingBytes.Length);
#else
            stream.Write(endingBytes.AsSpan());
#endif
            stream.Flush();
        }

        public override async Task FlushAsync(CancellationToken cancellationToken)
        {
            if (ended)
                throw new CqrsNetworkException("body already ended");
            ended = true;
#if NETSTANDARD2_0
            await stream.WriteAsync(endingBytes, 0, endingBytes.Length, cancellationToken);
#else
            await stream.WriteAsync(endingBytes.AsMemory(), cancellationToken);
#endif
            await stream.FlushAsync(cancellationToken);
        }
    }
}