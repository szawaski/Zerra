// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Buffers;
using System.Buffers.Text;
using System.Globalization;
using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Zerra.Buffers;
using Zerra.IO;

namespace Zerra.CQRS.Network
{
    internal sealed class HttpProtocolBodyStream : StreamTransform
    {
        private static readonly Encoding encoding = Encoding.UTF8;
        private static readonly byte[] newLineBytes = encoding.GetBytes("\r\n");
        private static readonly byte[] endingBytes = encoding.GetBytes("0\r\n\r\n");

        private readonly int? contentLength;
        private readonly ReadOnlyMemory<byte> readStartBuffer;
        private readonly bool writeMode;
        private readonly bool endOnPeerSilence;
        private int readStartBufferPosition;
        private long position;
        private int segmentPosition;
        private int segmentLength;
        private bool ended;
        private const int segmentLengthBufferMaxLength = 24;
        private byte[]? segmentLengthBufferSource;
        private int segmentLengthBufferLength;
        private int segmentLengthBufferPosition;

        //chunked writes are buffered so a chunk's length, data, and the ending go out in one write instead of many small packets
        private const int writeBufferLength = 1024 * 16;
        //8 hex digits and a line break, leading zeros are allowed
        private const int standardChunkHeaderLength = 10;
        private static readonly StandardFormat standardChunkLengthFormat = new('X', 8);
        //20 hex digits and a line break
        //5.3 clients read a chunk's length into a 24 byte buffer and wait on the socket before looking at what they already have,
        //a length line this long leaves them at most two extra bytes so the body ending can never sit unseen in that buffer,
        //a longer line would not fit in their buffer along with the previous chunk's line break
        //only responses use it, Kestrel rejects a length line this long so requests stay standard
        private const int wideChunkHeaderLength = 22;
        private static readonly StandardFormat wideChunkLengthFormat = new('X', 20);
        private readonly int writeChunkHeaderLength;
        private readonly StandardFormat writeChunkLengthFormat;
        private byte[]? writeBufferSource;
        private int writeSegmentStart;
        private int writeBufferPosition;

        public HttpProtocolBodyStream(int? contentLength, Stream stream, ReadOnlyMemory<byte> readStartBuffer, bool writeMode, bool leaveOpen, ReadOnlyMemory<byte> writePrefix = default, bool wideChunkLengths = false, bool endOnPeerSilence = false) : base(stream, leaveOpen)
        {
            if (writePrefix.Length > 0 && (!writeMode || contentLength.HasValue))
                throw new ArgumentException("A write prefix requires a chunked body in write mode", nameof(writePrefix));

            this.endOnPeerSilence = endOnPeerSilence;

            this.writeChunkHeaderLength = wideChunkLengths ? wideChunkHeaderLength : standardChunkHeaderLength;
            this.writeChunkLengthFormat = wideChunkLengths ? wideChunkLengthFormat : standardChunkLengthFormat;

            this.contentLength = contentLength;
            this.readStartBuffer = readStartBuffer;
            this.writeMode = writeMode;
            this.readStartBufferPosition = 0;
            this.segmentPosition = 0;
            this.segmentLength = -1;
            this.segmentLengthBufferLength = 0;
            this.segmentLengthBufferPosition = 0;
            this.ended = false;
            if (!contentLength.HasValue)
            {
                this.segmentLengthBufferSource = ArrayPoolHelper<byte>.Rent(segmentLengthBufferMaxLength);
            }
            else
            {
                this.segmentLengthBufferSource = null;
            }
            if (writeMode && !contentLength.HasValue)
            {
                //the prefix such as a header goes out with the first write
                this.writeBufferSource = ArrayPoolHelper<byte>.Rent(writePrefix.Length + writeBufferLength);
                writePrefix.Span.CopyTo(this.writeBufferSource);
                this.writeSegmentStart = writePrefix.Length;
                this.writeBufferPosition = writeSegmentStart + writeChunkHeaderLength;
            }
        }

        public override bool CanRead => !writeMode;
        public override bool CanWrite => writeMode;
        public override bool CanSeek => false;

        protected override void Dispose(bool disposing)
        {
            if (segmentLengthBufferSource is not null)
            {
                ArrayPoolHelper<byte>.Return(segmentLengthBufferSource);
                segmentLengthBufferSource = null;
            }
            if (writeBufferSource is not null)
            {
                ArrayPoolHelper<byte>.Return(writeBufferSource);
                writeBufferSource = null;
            }
            base.Dispose(disposing);
        }
#if !NETSTANDARD2_0
        public override ValueTask DisposeAsync()
        {
            if (segmentLengthBufferSource is not null)
            {
                ArrayPoolHelper<byte>.Return(segmentLengthBufferSource);
                segmentLengthBufferSource = null;
            }
            if (writeBufferSource is not null)
            {
                ArrayPoolHelper<byte>.Return(writeBufferSource);
                writeBufferSource = null;
            }
            return base.DisposeAsync();
        }
#endif

        public override long Length => contentLength ?? default;
        public override long Position { get { return position; } set { throw new NotSupportedException(); } }

        public override long Seek(long offset, SeekOrigin origin) { throw new NotSupportedException(); }
        public override void SetLength(long value) { throw new NotSupportedException(); }

        //5.3 servers never end an unencrypted body so the ending waited on here would never come
        //a peer that sent it has it waiting already, only a silent one waits out the full time, and its body is over
        //this is only for a body read into an object, a stream handed to the caller must not end early and lose data
        private const int peerSilenceWaitMicroseconds = 3 * 1000 * 1000;
        private bool PeerWentSilent() => endOnPeerSilence && stream is SocketPoolStream socketStream && !socketStream.WaitForRead(peerSilenceWaitMicroseconds);
        private async ValueTask<bool> PeerWentSilentAsync(CancellationToken cancellationToken) => endOnPeerSilence && stream is SocketPoolStream socketStream && !await socketStream.WaitForReadAsync(peerSilenceWaitMicroseconds, cancellationToken);

        protected override int InternalRead(Span<byte> buffer)
        {
            if (writeMode)
                throw new InvalidOperationException("Stream is in write mode");

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
                        var read = stream.ReadToSpan(buffer.Slice(bytesRead, bytesToRead - bytesRead));
#else
                        var read = stream.Read(buffer.Slice(bytesRead, bytesToRead - bytesRead));
#endif
                        if (read == 0)
                            throw new ConnectionAbortedException();
                        bytesRead += read;
                    }
                }
                position += bytesRead;
                return bytesRead;
            }
            else
            {
                var totalBytesRead = 0;
                int bytesToRead;
                while (totalBytesRead < buffer.Length)
                {
                    int bytesRead;
                    if (segmentLength < 0)
                    {
                        if (segmentLengthBufferPosition < segmentLengthBufferLength)
                        {
                            segmentLengthBufferSource.AsSpan(segmentLengthBufferPosition, segmentLengthBufferLength - segmentLengthBufferPosition).CopyTo(segmentLengthBufferSource);
                            segmentLengthBufferLength -= segmentLengthBufferPosition;
                            segmentLengthBufferPosition = 0;
                        }
                        else
                        {
                            segmentLengthBufferLength = 0;
                            segmentLengthBufferPosition = 0;
                        }

                        var segmentLengthStringStart = 0;
                        var segmentLengthStringEnd = 0;
                        var peerEnded = false;
                        var readBuffered = segmentLengthBufferLength > 0; //leftover bytes may already hold the segment length, reading first would wait on data that never comes
                        while (segmentLengthBufferLength < segmentLengthBufferMaxLength)
                        {
                            if (segmentLengthBufferLength == segmentLengthBufferMaxLength)
                                throw new ConnectionAbortedException();

                            if (readBuffered)
                            {
                                readBuffered = false;
                                bytesRead = 0;
                            }
                            else if (readStartBufferPosition < readStartBuffer.Length)
                            {
                                bytesToRead = Math.Min(readStartBuffer.Length - readStartBufferPosition, segmentLengthBufferMaxLength - segmentLengthBufferLength);
                                readStartBuffer.Span.Slice(readStartBufferPosition, bytesToRead).CopyTo(segmentLengthBufferSource.AsSpan(segmentLengthBufferLength, bytesToRead));
                                readStartBufferPosition += bytesToRead;
                                bytesRead = bytesToRead;
                            }
                            else
                            {
                                if (PeerWentSilent())
                                {
                                    peerEnded = true;
                                    break;
                                }
#if NETSTANDARD2_0
                                bytesRead = stream.Read(segmentLengthBufferSource, segmentLengthBufferLength, segmentLengthBufferMaxLength - segmentLengthBufferLength);
#else
                                bytesRead = stream.Read(segmentLengthBufferSource.AsSpan(segmentLengthBufferLength, segmentLengthBufferMaxLength - segmentLengthBufferLength));
#endif
                                if (bytesRead == 0)
                                    throw new ConnectionAbortedException();
                            }

                            segmentLengthBufferLength += bytesRead;

                            if (HttpCommon.ReadToBreak(segmentLengthBufferSource.AsSpan(0, segmentLengthBufferLength), ref segmentLengthBufferPosition))
                            {
                                if (segmentLength == -2)
                                {
                                    segmentLengthStringStart = segmentLengthBufferPosition;
                                    if (!HttpCommon.ReadToBreak(segmentLengthBufferSource.AsSpan(0, segmentLengthBufferLength), ref segmentLengthBufferPosition))
                                    {
                                        segmentLengthBufferPosition = 0;
                                        continue;
                                    }
                                }

                                segmentLengthStringEnd = segmentLengthBufferPosition - 2;

                                //the last segment "0" is followed by the line break ending the body, consumed so a reused connection doesn't start with it
                                if (segmentLengthStringEnd - segmentLengthStringStart != 1 || segmentLengthBufferSource![segmentLengthStringStart] != '0' || HttpCommon.ReadToBreak(segmentLengthBufferSource.AsSpan(0, segmentLengthBufferLength), ref segmentLengthBufferPosition))
                                    break;
                                segmentLengthBufferPosition = 0;
                            }
                        }

                        if (peerEnded)
                        {
                            segmentLength = 0; //the body is over, later reads end without waiting again
                            break;
                        }

#if NETSTANDARD2_0
                        var segmentLengthString = encoding.GetString(segmentLengthBufferSource, segmentLengthStringStart, segmentLengthStringEnd - segmentLengthStringStart);
#else
                        var segmentLengthString = encoding.GetString(segmentLengthBufferSource.AsSpan(segmentLengthStringStart, segmentLengthStringEnd - segmentLengthStringStart));
#endif
                        segmentLength = Int32.Parse(segmentLengthString, NumberStyles.HexNumber);
                        if (segmentLength < 0)
                            throw new CqrsNetworkException("Bad Data");
                    }

                    if (segmentLength == 0)
                        break;

                    bytesToRead = Math.Min(buffer.Length - totalBytesRead, segmentLength - segmentPosition);

                    if (segmentLengthBufferPosition < segmentLengthBufferLength)
                    {
                        bytesToRead = Math.Min(segmentLengthBufferLength - segmentLengthBufferPosition, bytesToRead);
                        segmentLengthBufferSource.AsSpan(segmentLengthBufferPosition, bytesToRead).CopyTo(buffer.Slice(totalBytesRead, bytesToRead));
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
                        if (bytesRead == 0)
                            throw new ConnectionAbortedException(); //the chunk isn't complete
                    }

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
            if (writeMode)
                throw new InvalidOperationException("Stream is in write mode");

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
                        var read = await stream.ReadToMemoryAsync(buffer.Slice(bytesRead, bytesToRead - bytesRead), cancellationToken);
#else
                        var read = await stream.ReadAsync(buffer.Slice(bytesRead, bytesToRead - bytesRead), cancellationToken);
#endif
                        if (read == 0)
                            throw new ConnectionAbortedException();
                        bytesRead += read;
                    }
                }
                position += bytesRead;
                return bytesRead;
            }
            else
            {
                var totalBytesRead = 0;
                int bytesToRead;
                while (totalBytesRead < buffer.Length)
                {
                    int bytesRead;

                    if (segmentLength < 0)
                    {
                        if (segmentLengthBufferPosition < segmentLengthBufferLength)
                        {
                            segmentLengthBufferSource.AsSpan(segmentLengthBufferPosition, segmentLengthBufferLength - segmentLengthBufferPosition).CopyTo(segmentLengthBufferSource);
                            segmentLengthBufferLength -= segmentLengthBufferPosition;
                            segmentLengthBufferPosition = 0;
                        }
                        else
                        {
                            segmentLengthBufferLength = 0;
                            segmentLengthBufferPosition = 0;
                        }

                        var segmentLengthStringStart = 0;
                        var segmentLengthStringEnd = 0;
                        var peerEnded = false;
                        var readBuffered = segmentLengthBufferLength > 0; //leftover bytes may already hold the segment length, reading first would wait on data that never comes
                        while (segmentLengthBufferLength < segmentLengthBufferMaxLength)
                        {
                            if (segmentLengthBufferLength == segmentLengthBufferMaxLength)
                                throw new CqrsNetworkException();

                            if (readBuffered)
                            {
                                readBuffered = false;
                                bytesRead = 0;
                            }
                            else if (readStartBufferPosition < readStartBuffer.Length)
                            {
                                bytesToRead = Math.Min(readStartBuffer.Length - readStartBufferPosition, segmentLengthBufferMaxLength - segmentLengthBufferLength);
                                readStartBuffer.Span.Slice(readStartBufferPosition, bytesToRead).CopyTo(segmentLengthBufferSource.AsSpan(segmentLengthBufferLength, bytesToRead));
                                readStartBufferPosition += bytesToRead;
                                bytesRead = bytesToRead;
                            }
                            else
                            {
                                if (await PeerWentSilentAsync(cancellationToken))
                                {
                                    peerEnded = true;
                                    break;
                                }
#if NETSTANDARD2_0
                                bytesRead = await stream.ReadAsync(segmentLengthBufferSource, segmentLengthBufferLength, segmentLengthBufferMaxLength - segmentLengthBufferLength, cancellationToken);
#else
                                bytesRead = await stream.ReadAsync(segmentLengthBufferSource.AsMemory(segmentLengthBufferLength, segmentLengthBufferMaxLength - segmentLengthBufferLength), cancellationToken);
#endif
                                if (bytesRead == 0)
                                    throw new ConnectionAbortedException();
                            }

                            segmentLengthBufferLength += bytesRead;

                            if (HttpCommon.ReadToBreak(segmentLengthBufferSource.AsSpan(0, segmentLengthBufferLength), ref segmentLengthBufferPosition))
                            {
                                if (segmentLength == -2)
                                {
                                    segmentLengthStringStart = segmentLengthBufferPosition;
                                    if (!HttpCommon.ReadToBreak(segmentLengthBufferSource.AsSpan(0, segmentLengthBufferLength), ref segmentLengthBufferPosition))
                                    {
                                        segmentLengthBufferPosition = 0;
                                        continue;
                                    }
                                }

                                segmentLengthStringEnd = segmentLengthBufferPosition - 2;

                                //the last segment "0" is followed by the line break ending the body, consumed so a reused connection doesn't start with it
                                if (segmentLengthStringEnd - segmentLengthStringStart != 1 || segmentLengthBufferSource![segmentLengthStringStart] != '0' || HttpCommon.ReadToBreak(segmentLengthBufferSource.AsSpan(0, segmentLengthBufferLength), ref segmentLengthBufferPosition))
                                    break;
                                segmentLengthBufferPosition = 0;
                            }
                        }

                        if (peerEnded)
                        {
                            segmentLength = 0; //the body is over, later reads end without waiting again
                            break;
                        }

#if NETSTANDARD2_0
                        var segmentLengthString = encoding.GetString(segmentLengthBufferSource, segmentLengthStringStart, segmentLengthStringEnd - segmentLengthStringStart);
#else
                        var segmentLengthString = encoding.GetString(segmentLengthBufferSource.AsSpan(segmentLengthStringStart, segmentLengthStringEnd - segmentLengthStringStart));
#endif
                        segmentLength = Int32.Parse(segmentLengthString, NumberStyles.HexNumber);
                        if (segmentLength < 0)
                            throw new CqrsNetworkException("Bad Data");
                    }

                    if (segmentLength == 0)
                        break;

                    bytesToRead = Math.Min(buffer.Length - totalBytesRead, segmentLength - segmentPosition);

                    if (segmentLengthBufferPosition < segmentLengthBufferLength)
                    {
                        bytesToRead = Math.Min(segmentLengthBufferLength - segmentLengthBufferPosition, bytesToRead);
                        segmentLengthBufferSource.AsSpan(segmentLengthBufferPosition, bytesToRead).CopyTo(buffer.Span.Slice(totalBytesRead, bytesToRead));
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
                        if (bytesRead == 0)
                            throw new ConnectionAbortedException(); //the chunk isn't complete
                    }

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
            if (!writeMode)
                throw new InvalidOperationException("Stream is not in write mode");

            if (contentLength.HasValue)
            {
#if NETSTANDARD2_0
                stream.Write(buffer.ToArray(), 0, buffer.Length);
#else
                stream.Write(buffer);
#endif
                position += buffer.Length;
            }
            else
            {
                position += buffer.Length;
                while (buffer.Length > 0)
                {
                    //room is kept for the chunk's line break and the ending so flush can send it with the last chunk
                    var bytesToCopy = Math.Min(buffer.Length, writeBufferSource!.Length - newLineBytes.Length - endingBytes.Length - writeBufferPosition);
                    buffer.Slice(0, bytesToCopy).CopyTo(writeBufferSource.AsSpan(writeBufferPosition));
                    writeBufferPosition += bytesToCopy;
                    buffer = buffer.Slice(bytesToCopy);

                    if (writeBufferPosition == writeBufferSource!.Length - newLineBytes.Length - endingBytes.Length)
                    {
                        _ = Utf8Formatter.TryFormat(writeBufferPosition - writeSegmentStart - writeChunkHeaderLength, writeBufferSource.AsSpan(writeSegmentStart), out _, writeChunkLengthFormat);
                        newLineBytes.CopyTo(writeBufferSource.AsSpan(writeSegmentStart + writeChunkHeaderLength - newLineBytes.Length));
                        newLineBytes.CopyTo(writeBufferSource.AsSpan(writeBufferPosition));
                        writeBufferPosition += newLineBytes.Length;
#if NETSTANDARD2_0
                        stream.Write(writeBufferSource, 0, writeBufferPosition);
#else
                        stream.Write(writeBufferSource.AsSpan(0, writeBufferPosition));
#endif
                        writeSegmentStart = 0;
                        writeBufferPosition = writeChunkHeaderLength;
                    }
                }
            }
        }
        protected override async ValueTask InternalWriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
        {
            if (!writeMode)
                throw new InvalidOperationException("Stream is not in write mode");

            if (contentLength.HasValue)
            {
#if NETSTANDARD2_0
                await stream.WriteAsync(buffer.ToArray(), 0, buffer.Length, cancellationToken);
#else
                await stream.WriteAsync(buffer, cancellationToken);
#endif
                position += buffer.Length;
            }
            else
            {
                position += buffer.Length;
                while (buffer.Length > 0)
                {
                    //room is kept for the chunk's line break and the ending so flush can send it with the last chunk
                    var bytesToCopy = Math.Min(buffer.Length, writeBufferSource!.Length - newLineBytes.Length - endingBytes.Length - writeBufferPosition);
                    buffer.Span.Slice(0, bytesToCopy).CopyTo(writeBufferSource.AsSpan(writeBufferPosition));
                    writeBufferPosition += bytesToCopy;
                    buffer = buffer.Slice(bytesToCopy);

                    if (writeBufferPosition == writeBufferSource!.Length - newLineBytes.Length - endingBytes.Length)
                    {
                        _ = Utf8Formatter.TryFormat(writeBufferPosition - writeSegmentStart - writeChunkHeaderLength, writeBufferSource.AsSpan(writeSegmentStart), out _, writeChunkLengthFormat);
                        newLineBytes.CopyTo(writeBufferSource.AsSpan(writeSegmentStart + writeChunkHeaderLength - newLineBytes.Length));
                        newLineBytes.CopyTo(writeBufferSource.AsSpan(writeBufferPosition));
                        writeBufferPosition += newLineBytes.Length;
#if NETSTANDARD2_0
                        await stream.WriteAsync(writeBufferSource, 0, writeBufferPosition, cancellationToken);
#else
                        await stream.WriteAsync(writeBufferSource.AsMemory(0, writeBufferPosition), cancellationToken);
#endif
                        writeSegmentStart = 0;
                        writeBufferPosition = writeChunkHeaderLength;
                    }
                }
            }
        }

        public override void Flush()
        {
            if (ended)
                throw new CqrsNetworkException("body already ended");
            ended = true;

            if (writeMode)
            {
                //a content length body is complete as written, the ending is only for chunked
                if (!contentLength.HasValue)
                {
                    //the last chunk and the ending go out together, without data the ending takes the chunk's place
                    var dataLength = writeBufferPosition - writeSegmentStart - writeChunkHeaderLength;
                    if (dataLength > 0)
                    {
                        _ = Utf8Formatter.TryFormat(dataLength, writeBufferSource.AsSpan(writeSegmentStart), out _, writeChunkLengthFormat);
                        newLineBytes.CopyTo(writeBufferSource.AsSpan(writeSegmentStart + writeChunkHeaderLength - newLineBytes.Length));
                        newLineBytes.CopyTo(writeBufferSource.AsSpan(writeBufferPosition));
                        writeBufferPosition += newLineBytes.Length;
                    }
                    else
                    {
                        writeBufferPosition = writeSegmentStart;
                    }
                    endingBytes.CopyTo(writeBufferSource.AsSpan(writeBufferPosition));
                    writeBufferPosition += endingBytes.Length;

#if NETSTANDARD2_0
                    stream.Write(writeBufferSource, 0, writeBufferPosition);
#else
                    stream.Write(writeBufferSource.AsSpan(0, writeBufferPosition));
#endif
                }
                stream.Flush();
            }
        }

        public override async Task FlushAsync(CancellationToken cancellationToken)
        {
            if (ended)
                throw new CqrsNetworkException("body already ended");
            ended = true;

            if (writeMode)
            {
                //a content length body is complete as written, the ending is only for chunked
                if (!contentLength.HasValue)
                {
                    //the last chunk and the ending go out together, without data the ending takes the chunk's place
                    var dataLength = writeBufferPosition - writeSegmentStart - writeChunkHeaderLength;
                    if (dataLength > 0)
                    {
                        _ = Utf8Formatter.TryFormat(dataLength, writeBufferSource.AsSpan(writeSegmentStart), out _, writeChunkLengthFormat);
                        newLineBytes.CopyTo(writeBufferSource.AsSpan(writeSegmentStart + writeChunkHeaderLength - newLineBytes.Length));
                        newLineBytes.CopyTo(writeBufferSource.AsSpan(writeBufferPosition));
                        writeBufferPosition += newLineBytes.Length;
                    }
                    else
                    {
                        writeBufferPosition = writeSegmentStart;
                    }
                    endingBytes.CopyTo(writeBufferSource.AsSpan(writeBufferPosition));
                    writeBufferPosition += endingBytes.Length;

#if NETSTANDARD2_0
                    await stream.WriteAsync(writeBufferSource, 0, writeBufferPosition, cancellationToken);
#else
                    await stream.WriteAsync(writeBufferSource.AsMemory(0, writeBufferPosition), cancellationToken);
#endif
                }
                await stream.FlushAsync(cancellationToken);
            }
        }
    }
}