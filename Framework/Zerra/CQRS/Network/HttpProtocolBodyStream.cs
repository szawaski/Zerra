// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Zerra.IO;

namespace Zerra.CQRS.Network
{
    public class HttpProtocolBodyStream : StreamTransform
    {
        private static readonly Encoding encoding = Encoding.UTF8;
        private static readonly byte[] newLineBytes = encoding.GetBytes("\r\n");
        private static readonly byte[] endingBytes = encoding.GetBytes("0\r\n");

        private readonly int? contentLength;
        private readonly ReadOnlyMemory<byte> readStartBuffer;
        private int readStartBufferPosition;
        private long position;
        private int segmentPosition;
        private int segmentLength;
        private bool ended;
        private const int segmentLengthBufferMaxLength = 24;
        private byte[] segmentLengthBufferSource;
        private Memory<byte> segmentLengthBuffer;
        private int segmentLengthBufferLength;
        private int segmentLengthBufferPosition;

        public HttpProtocolBodyStream(int? contentLength, Stream stream, ReadOnlyMemory<byte> readStartBuffer, bool leaveOpen) : base(stream, leaveOpen)
        {
            this.contentLength = contentLength;
            this.readStartBuffer = readStartBuffer;
            this.readStartBufferPosition = 0;
            this.segmentPosition = 0;
            this.segmentLength = -1;
            this.segmentLengthBufferLength = 0;
            this.segmentLengthBufferPosition = 0;
            this.ended = false;
            if (!contentLength.HasValue)
            {
                this.segmentLengthBufferSource = BufferArrayPool<byte>.Rent(segmentLengthBufferMaxLength);
                this.segmentLengthBuffer = segmentLengthBufferSource;
            }
            else
            {
                this.segmentLengthBufferSource = null;
                this.segmentLengthBuffer = null;
            }
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

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => contentLength.Value;
        public override long Position { get { return position; } set { throw new NotSupportedException(); } }

        public override long Seek(long offset, SeekOrigin origin) { throw new NotSupportedException(); }
        public override void SetLength(long value) { throw new NotSupportedException(); }

        protected override int InternalRead(Span<byte> buffer)
        {
            if (contentLength.HasValue)
            {
                var bytesToRead = (int)Math.Min(buffer.Length, contentLength.Value - position);
                if (bytesToRead == 0)
                    return 0;

                var bytesRead = 0;
                while (bytesRead < bytesToRead)
                {
                    if (readStartBufferPosition < readStartBuffer.Length)
                    {
                        bytesToRead = Math.Min(readStartBuffer.Length - readStartBufferPosition, bytesToRead);
                        readStartBuffer.Span.Slice(readStartBufferPosition, bytesToRead).CopyTo(buffer.Slice(bytesRead, bytesToRead - bytesRead));
                        readStartBufferPosition += bytesToRead;
                        bytesRead += bytesToRead;
                    }
                    else
                    {
#if NETSTANDARD2_0
                        bytesRead += stream.ReadToSpan(buffer.Slice(bytesRead, bytesToRead - bytesRead));
#else
                        bytesRead += stream.Read(buffer.Slice(bytesRead, bytesToRead - bytesRead));
#endif
                    }
                    if (bytesRead == 0)
                        throw new EndOfStreamException();

                    position += bytesRead;
                }
                return bytesRead;
            }
            else
            {
                var totalBytesRead = 0;
                int bytesToRead;
                while (totalBytesRead < buffer.Length)
                {
                    int bytesRead;
                    var segmentLengthStringStart = 0;
                    if (segmentLength < 0)
                    {
                        segmentLengthBufferLength = 0;
                        segmentLengthBufferPosition = 0;
                        segmentLengthStringStart = 0;
                        while (segmentLengthBufferLength < segmentLengthBufferMaxLength)
                        {
                            if (segmentLengthBufferLength == segmentLengthBufferMaxLength)
                                throw new EndOfStreamException();

                            if (readStartBufferPosition < readStartBuffer.Length)
                            {
                                bytesToRead = Math.Min(readStartBuffer.Length - readStartBufferPosition, segmentLengthBufferMaxLength - segmentLengthBufferLength);
                                readStartBuffer.Slice(readStartBufferPosition, bytesToRead).CopyTo(segmentLengthBuffer.Slice(segmentLengthBufferLength, bytesToRead));
                                readStartBufferPosition += bytesToRead;
                                bytesRead = bytesToRead;
                                if (bytesRead == 0)
                                    throw new EndOfStreamException();
                            }
                            else
                            {
#if NETSTANDARD2_0
                                bytesRead = stream.Read(segmentLengthBufferSource, segmentLengthBufferLength, segmentLengthBufferMaxLength - segmentLengthBufferLength);
#else
                                bytesRead = stream.Read(segmentLengthBuffer.Span[segmentLengthBufferLength..segmentLengthBufferMaxLength]);
#endif
                                if (bytesRead == 0)
                                    throw new EndOfStreamException();
                            }

                            segmentLengthBufferLength += bytesRead;

                            if (HttpCommon.ReadToBreak(segmentLengthBuffer, ref segmentLengthBufferPosition, segmentLengthBufferLength))
                            {
                                if (segmentLength == -2)
                                {
                                    segmentLengthStringStart = segmentLengthBufferPosition;
                                    if (!HttpCommon.ReadToBreak(segmentLengthBuffer, ref segmentLengthBufferPosition, segmentLengthBufferLength))
                                        segmentLengthBufferPosition = 0;
                                    else
                                        break;
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }

#if NETSTANDARD2_0
                        var segmentLengthString = encoding.GetString(segmentLengthBufferSource, segmentLengthStringStart, segmentLengthBufferPosition - segmentLengthStringStart - 2);
#else
                        var segmentLengthString = encoding.GetString(segmentLengthBuffer.Span.Slice(segmentLengthStringStart, segmentLengthBufferPosition - segmentLengthStringStart - 2));
#endif
                        segmentLength = Int32.Parse(segmentLengthString);
                        if (segmentLength < 0)
                            throw new Exception("Bad Data");
                    }

                    if (segmentLength == 0)
                        break;

                    bytesToRead = Math.Min(buffer.Length - totalBytesRead, segmentLength - segmentPosition);

                    if (segmentLengthBufferPosition < segmentLengthBufferLength)
                    {
                        bytesToRead = Math.Min(segmentLengthBufferLength - segmentLengthBufferPosition, bytesToRead);
                        segmentLengthBuffer.Span.Slice(segmentLengthBufferPosition, bytesToRead).CopyTo(buffer.Slice(totalBytesRead, bytesToRead));
                        segmentLengthBufferPosition += bytesToRead;
                        bytesRead = bytesToRead;
                    }
                    else if (readStartBufferPosition < readStartBuffer.Length)
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
                    if (bytesRead == 0)
                        break;

                    segmentPosition += bytesRead;
                    totalBytesRead += bytesRead;

                    if (segmentPosition == segmentLength)
                    {
                        segmentPosition = 0;
                        segmentLength = -2;
                    }
                }

                position += totalBytesRead;
                return totalBytesRead;
            }
        }
        protected override async ValueTask<int> InternalReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            if (contentLength.HasValue)
            {
                var bytesToRead = (int)Math.Min(buffer.Length, contentLength.Value - position);
                if (bytesToRead == 0)
                    return 0;

                var bytesRead = 0;
                while (bytesRead < bytesToRead)
                {
                    if (readStartBufferPosition < readStartBuffer.Length)
                    {
                        bytesToRead = Math.Min(readStartBuffer.Length - readStartBufferPosition, bytesToRead);
                        readStartBuffer.Slice(readStartBufferPosition, bytesToRead).CopyTo(buffer.Slice(bytesRead, bytesToRead - bytesRead));
                        readStartBufferPosition += bytesToRead;
                        bytesRead += bytesToRead;
                    }
                    else
                    {
#if NETSTANDARD2_0
                        bytesRead += await stream.ReadToMemoryAsync(buffer.Slice(bytesRead, bytesToRead - bytesRead), cancellationToken);
#else
                        bytesRead += await stream.ReadAsync(buffer.Slice(bytesRead, bytesToRead - bytesRead), cancellationToken);
#endif
                    }
                    if (bytesRead == 0)
                        throw new EndOfStreamException();

                    position += bytesRead;
                }
                return bytesRead;
            }
            else
            {
                var totalBytesRead = 0;
                int bytesToRead;
                while (totalBytesRead < buffer.Length)
                {
                    int bytesRead;
                    var segmentLengthStringStart = 0;
                    if (segmentLength < 0)
                    {
                        segmentLengthBufferLength = 0;
                        segmentLengthBufferPosition = 0;
                        segmentLengthStringStart = 0;
                        while (segmentLengthBufferLength < segmentLengthBufferMaxLength)
                        {
                            if (segmentLengthBufferLength == segmentLengthBufferMaxLength)
                                throw new EndOfStreamException();

                            if (readStartBufferPosition < readStartBuffer.Length)
                            {
                                bytesToRead = Math.Min(readStartBuffer.Length - readStartBufferPosition, segmentLengthBufferMaxLength - segmentLengthBufferLength);
                                readStartBuffer.Slice(readStartBufferPosition, bytesToRead).CopyTo(segmentLengthBuffer.Slice(segmentLengthBufferLength, bytesToRead));
                                readStartBufferPosition += bytesToRead;
                                bytesRead = bytesToRead;
                                if (bytesRead == 0)
                                    throw new EndOfStreamException();
                            }
                            else
                            {
#if NETSTANDARD2_0
                                bytesRead = await stream.ReadAsync(segmentLengthBufferSource, segmentLengthBufferLength, segmentLengthBufferMaxLength - segmentLengthBufferLength, cancellationToken);
#else
                                bytesRead = await stream.ReadAsync(segmentLengthBuffer[segmentLengthBufferLength..segmentLengthBufferMaxLength], cancellationToken);
#endif
                                if (bytesRead == 0)
                                    throw new EndOfStreamException();
                            }

                            segmentLengthBufferLength += bytesRead;

                            if (HttpCommon.ReadToBreak(segmentLengthBuffer, ref segmentLengthBufferPosition, segmentLengthBufferLength))
                            {
                                if (segmentLength == -2)
                                {
                                    segmentLengthStringStart = segmentLengthBufferPosition;
                                    if (!HttpCommon.ReadToBreak(segmentLengthBuffer, ref segmentLengthBufferPosition, segmentLengthBufferLength))
                                        segmentLengthBufferPosition = 0;
                                    else
                                        break;
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }

#if NETSTANDARD2_0
                        var segmentLengthString = encoding.GetString(segmentLengthBufferSource, segmentLengthStringStart, segmentLengthBufferPosition - segmentLengthStringStart - 2);
#else
                        var segmentLengthString = encoding.GetString(segmentLengthBuffer.Span.Slice(segmentLengthStringStart, segmentLengthBufferPosition - segmentLengthStringStart - 2));
#endif
                        segmentLength = Int32.Parse(segmentLengthString);
                        if (segmentLength < 0)
                            throw new Exception("Bad Data");
                    }

                    if (segmentLength == 0)
                        break;

                    bytesToRead = Math.Min(buffer.Length - totalBytesRead, segmentLength - segmentPosition);

                    if (segmentLengthBufferPosition < segmentLengthBufferLength)
                    {
                        bytesToRead = Math.Min(segmentLengthBufferLength - segmentLengthBufferPosition, bytesToRead);
                        segmentLengthBuffer.Slice(segmentLengthBufferPosition, bytesToRead).CopyTo(buffer.Slice(totalBytesRead, bytesToRead));
                        segmentLengthBufferPosition += bytesToRead;
                        bytesRead = bytesToRead;
                    }
                    else if (readStartBufferPosition < readStartBuffer.Length)
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
                        segmentLength = -2;
                    }
                }

                position += totalBytesRead;
                return totalBytesRead;
            }
        }

        protected override unsafe void InternalWrite(ReadOnlySpan<byte> buffer)
        {
            if (contentLength.HasValue)
            {
#if NETSTANDARD2_0
                stream.Write(buffer.ToArray(), 0, buffer.Length);
#else
                stream.Write(buffer);
#endif
            }
            else
            {
                var lengthBytes = encoding.GetBytes(buffer.Length.ToString());
#if NETSTANDARD2_0
                stream.Write(lengthBytes, 0, lengthBytes.Length);
                stream.Write(newLineBytes, 0, newLineBytes.Length);
                stream.Write(buffer.ToArray(), 0, buffer.Length);
                stream.Write(newLineBytes, 0, newLineBytes.Length);
#else
                stream.Write(lengthBytes.AsSpan());
                stream.Write(newLineBytes.AsSpan());
                stream.Write(buffer);
                stream.Write(newLineBytes.AsSpan());
#endif
            }
        }
        protected override async ValueTask InternalWriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
        {
            if (contentLength.HasValue)
            {
#if NETSTANDARD2_0
                await stream.WriteAsync(buffer.ToArray(), 0, buffer.Length, cancellationToken);
#else
                await stream.WriteAsync(buffer, cancellationToken);
#endif
            }
            else
            {
                var lengthBytes = encoding.GetBytes(buffer.Length.ToString());
#if NETSTANDARD2_0
                await stream.WriteAsync(lengthBytes, 0, lengthBytes.Length);
                await stream.WriteAsync(newLineBytes, 0, newLineBytes.Length);
                await stream.WriteAsync(buffer.ToArray(), 0, buffer.Length);
                await stream.WriteAsync(newLineBytes, 0, newLineBytes.Length);
#else
                await stream.WriteAsync(lengthBytes.AsMemory(), cancellationToken);
                await stream.WriteAsync(newLineBytes.AsMemory(), cancellationToken);
                await stream.WriteAsync(buffer, cancellationToken);
                await stream.WriteAsync(newLineBytes.AsMemory(), cancellationToken);
#endif
            }
        }

        public override void Flush()
        {
            if (ended)
                throw new Exception("body already ended");
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
                throw new Exception("body already ended");
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