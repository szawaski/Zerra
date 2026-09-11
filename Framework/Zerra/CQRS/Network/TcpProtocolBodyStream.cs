// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Buffers;
using Zerra.IO;

namespace Zerra.CQRS.Network
{
    internal sealed class TcpProtocolBodyStream : StreamTransform
    {
        private static readonly byte[] endingBytes = BitConverter.GetBytes(0);

        private readonly ReadOnlyMemory<byte> readStartBuffer;
        private readonly bool writeMode;
        private int readStartBufferPosition;
        private long position;
        private int segmentPosition;
        private int segmentLength;
        private bool ended;
        private const int segmentLengthBufferLength = 4;
        private byte[]? segmentLengthBufferSource;

        //writes are buffered so a segment's length, data, and the ending go out in one write instead of many small packets
        //lengths are written with BitConverter to match the byte order the reader uses
        private const int writeBufferLength = 1024 * 16;
        private byte[]? writeBufferSource;
        private int writeSegmentStart;
        private int writeBufferPosition;

        public TcpProtocolBodyStream(Stream stream, ReadOnlyMemory<byte> readStartBufferPosition, bool writeMode, bool leaveOpen, ReadOnlyMemory<byte> writePrefix = default) : base(stream, leaveOpen)
        {
            this.readStartBuffer = readStartBufferPosition;
            this.writeMode = writeMode;
            this.readStartBufferPosition = 0;
            this.position = 0;
            this.segmentPosition = 0;
            this.segmentLength = -1;
            this.ended = false;
            this.segmentLengthBufferSource = ArrayPoolHelper<byte>.Rent(segmentLengthBufferLength);
            if (writeMode)
            {
                //the prefix such as a header goes out with the first write
                this.writeBufferSource = ArrayPoolHelper<byte>.Rent(writePrefix.Length + writeBufferLength);
                writePrefix.Span.CopyTo(this.writeBufferSource);
                this.writeSegmentStart = writePrefix.Length;
                this.writeBufferPosition = writeSegmentStart + segmentLengthBufferLength;
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

        public override long Length => throw new NotSupportedException();
        public override long Position { get { return position; } set { throw new NotSupportedException(); } }

        public override long Seek(long offset, SeekOrigin origin) { throw new NotSupportedException(); }
        public override void SetLength(long value) { throw new NotSupportedException(); }

        protected override int InternalRead(Span<byte> buffer)
        {
            if (writeMode)
                throw new InvalidOperationException("Stream is in write mode");

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
                            readStartBuffer.Span.Slice(readStartBufferPosition, bytesToRead).CopyTo(segmentLengthBufferSource.AsSpan(bytesRead, bytesToRead));
                            readStartBufferPosition += bytesToRead;
                            bytesRead += bytesToRead;
                        }
                        else
                        {
#if NETSTANDARD2_0
                            bytesRead += stream.Read(segmentLengthBufferSource, bytesRead, segmentLengthBufferLength - bytesRead);
#else
                            bytesRead += stream.Read(segmentLengthBufferSource.AsSpan(bytesRead, segmentLengthBufferLength - bytesRead));
#endif
                        }

                        if (bytesRead == 0)
                            throw new ConnectionAbortedException();
                    }
#if NETSTANDARD2_0
                    segmentLength = BitConverter.ToInt32(segmentLengthBufferSource, 0);
#else
                    segmentLength = BitConverter.ToInt32(segmentLengthBufferSource.AsSpan());
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
            if (writeMode)
                throw new InvalidOperationException("Stream is in write mode");

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
                            readStartBuffer.Span.Slice(readStartBufferPosition, bytesToRead).CopyTo(segmentLengthBufferSource.AsSpan(bytesRead, bytesToRead));
                            readStartBufferPosition += bytesToRead;
                            bytesRead += bytesToRead;
                        }
                        else
                        {
#if NETSTANDARD2_0
                            bytesRead += await stream.ReadAsync(segmentLengthBufferSource, bytesRead, segmentLengthBufferLength - bytesRead);
#else
                            bytesRead += await stream.ReadAsync(segmentLengthBufferSource.AsMemory(bytesRead, segmentLengthBufferLength - bytesRead), cancellationToken);
#endif
                        }

                        if (bytesRead == 0)
                            throw new ConnectionAbortedException();
                    }
#if NETSTANDARD2_0
                    segmentLength = BitConverter.ToInt32(segmentLengthBufferSource, 0);
#else
                    segmentLength = BitConverter.ToInt32(segmentLengthBufferSource.AsSpan());
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
            if (!writeMode)
                throw new InvalidOperationException("Stream is not in write mode");

            position += buffer.Length;
            while (buffer.Length > 0)
            {
                //room is kept for the ending so flush can send it with the last segment
                var bytesToCopy = Math.Min(buffer.Length, writeBufferSource!.Length - endingBytes.Length - writeBufferPosition);
                buffer.Slice(0, bytesToCopy).CopyTo(writeBufferSource.AsSpan(writeBufferPosition));
                writeBufferPosition += bytesToCopy;
                buffer = buffer.Slice(bytesToCopy);

                if (writeBufferPosition == writeBufferSource!.Length - endingBytes.Length)
                {
                    _ = BitConverter.TryWriteBytes(writeBufferSource.AsSpan(writeSegmentStart), writeBufferPosition - writeSegmentStart - segmentLengthBufferLength);
#if NETSTANDARD2_0
                    stream.Write(writeBufferSource, 0, writeBufferPosition);
#else
                    stream.Write(writeBufferSource.AsSpan(0, writeBufferPosition));
#endif
                    writeSegmentStart = 0;
                    writeBufferPosition = segmentLengthBufferLength;
                }
            }
        }

        protected override async ValueTask InternalWriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
        {
            if (!writeMode)
                throw new InvalidOperationException("Stream is not in write mode");

            position += buffer.Length;
            while (buffer.Length > 0)
            {
                //room is kept for the ending so flush can send it with the last segment
                var bytesToCopy = Math.Min(buffer.Length, writeBufferSource!.Length - endingBytes.Length - writeBufferPosition);
                buffer.Span.Slice(0, bytesToCopy).CopyTo(writeBufferSource.AsSpan(writeBufferPosition));
                writeBufferPosition += bytesToCopy;
                buffer = buffer.Slice(bytesToCopy);

                if (writeBufferPosition == writeBufferSource!.Length - endingBytes.Length)
                {
                    _ = BitConverter.TryWriteBytes(writeBufferSource.AsSpan(writeSegmentStart), writeBufferPosition - writeSegmentStart - segmentLengthBufferLength);
#if NETSTANDARD2_0
                    await stream.WriteAsync(writeBufferSource, 0, writeBufferPosition, cancellationToken);
#else
                    await stream.WriteAsync(writeBufferSource.AsMemory(0, writeBufferPosition), cancellationToken);
#endif
                    writeSegmentStart = 0;
                    writeBufferPosition = segmentLengthBufferLength;
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
                //the last segment and the ending go out together, without data the ending takes the segment's place
                var dataLength = writeBufferPosition - writeSegmentStart - segmentLengthBufferLength;
                if (dataLength > 0)
                    _ = BitConverter.TryWriteBytes(writeBufferSource.AsSpan(writeSegmentStart), dataLength);
                else
                    writeBufferPosition = writeSegmentStart;
                endingBytes.CopyTo(writeBufferSource.AsSpan(writeBufferPosition));
                writeBufferPosition += endingBytes.Length;

#if NETSTANDARD2_0
                stream.Write(writeBufferSource, 0, writeBufferPosition);
#else
                stream.Write(writeBufferSource.AsSpan(0, writeBufferPosition));
#endif
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
                //the last segment and the ending go out together, without data the ending takes the segment's place
                var dataLength = writeBufferPosition - writeSegmentStart - segmentLengthBufferLength;
                if (dataLength > 0)
                    _ = BitConverter.TryWriteBytes(writeBufferSource.AsSpan(writeSegmentStart), dataLength);
                else
                    writeBufferPosition = writeSegmentStart;
                endingBytes.CopyTo(writeBufferSource.AsSpan(writeBufferPosition));
                writeBufferPosition += endingBytes.Length;

#if NETSTANDARD2_0
                await stream.WriteAsync(writeBufferSource, 0, writeBufferPosition, cancellationToken);
#else
                await stream.WriteAsync(writeBufferSource.AsMemory(0, writeBufferPosition), cancellationToken);
#endif
                await stream.FlushAsync(cancellationToken);
            }
        }
    }
}