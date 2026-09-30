// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Compression;

namespace Zerra.Test.Compression
{
    //length prefixed with no compression, the decompress stream reads exactly its data and never reads the source again,
    //like a real decompressor reaching its end marker right where a read ends, so the transports have to read the body's ending themselves
    public sealed class ExactEndCompressor : ICompressor
    {
        public byte[] Compress(byte[] bytes)
        {
            var result = new byte[bytes.Length + 4];
            BitConverter.GetBytes(bytes.Length).CopyTo(result, 0);
            bytes.CopyTo(result, 4);
            return result;
        }

        public byte[] Decompress(byte[] bytes) => bytes.AsSpan(4, BitConverter.ToInt32(bytes, 0)).ToArray();

        public Span<byte> Compress(ReadOnlySpan<byte> bytes) => Compress(bytes.ToArray());

        public Span<byte> Decompress(ReadOnlySpan<byte> bytes) => Decompress(bytes.ToArray());

        public Stream Compress(Stream stream, bool leaveOpen) => new CompressStream(stream, leaveOpen);

        public Stream Decompress(Stream stream, bool leaveOpen) => new DecompressStream(stream, leaveOpen);

        private sealed class CompressStream : Stream
        {
            private readonly Stream stream;
            private readonly bool leaveOpen;
            private readonly MemoryStream data = new();

            public CompressStream(Stream stream, bool leaveOpen)
            {
                this.stream = stream;
                this.leaveOpen = leaveOpen;
            }

            public override bool CanRead => false;
            public override bool CanSeek => false;
            public override bool CanWrite => true;
            public override long Length => throw new NotSupportedException();
            public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
            public override void Flush() { }
            public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => data.Write(buffer, offset, count);

            protected override void Dispose(bool disposing)
            {
                if (disposing && data.CanRead)
                {
                    stream.Write(BitConverter.GetBytes((int)data.Length));
                    stream.Write(data.GetBuffer(), 0, (int)data.Length);
                    data.Dispose();
                    if (!leaveOpen)
                        stream.Dispose();
                }
                base.Dispose(disposing);
            }
        }

        private sealed class DecompressStream : Stream
        {
            private readonly Stream stream;
            private readonly bool leaveOpen;
            private int remaining = -1;

            public DecompressStream(Stream stream, bool leaveOpen)
            {
                this.stream = stream;
                this.leaveOpen = leaveOpen;
            }

            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => throw new NotSupportedException();
            public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
            public override void Flush() { }
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

            public override int Read(byte[] buffer, int offset, int count)
            {
                if (remaining == -1)
                {
                    var length = new byte[4];
                    stream.ReadExactly(length);
                    remaining = BitConverter.ToInt32(length);
                }
                if (remaining == 0 || count == 0)
                    return 0;
                var read = stream.Read(buffer, offset, Math.Min(count, remaining));
                if (read == 0)
                    throw new EndOfStreamException();
                remaining -= read;
                return read;
            }

            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

            public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            {
                if (remaining == -1)
                {
                    var length = new byte[4];
                    await stream.ReadExactlyAsync(length, cancellationToken);
                    remaining = BitConverter.ToInt32(length);
                }
                if (remaining == 0 || buffer.Length == 0)
                    return 0;
                var read = await stream.ReadAsync(buffer.Slice(0, Math.Min(buffer.Length, remaining)), cancellationToken);
                if (read == 0)
                    throw new EndOfStreamException();
                remaining -= read;
                return read;
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing && !leaveOpen)
                    stream.Dispose();
                base.Dispose(disposing);
            }
        }
    }
}
