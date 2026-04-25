// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Security.Cryptography;
using Zerra.Buffers;
using Zerra.IO;

namespace Zerra.Encryption
{
    internal class CryptoPrefixStream : StreamTransform
    {
        private readonly CryptoStream? cryptoStream;
        private readonly int blockSizeBytes;
        private readonly int keySizeBytes;
        private readonly CryptoStreamMode mode;
        private readonly bool removePrefix;

        private byte[]? prefixBufferOwner;

        private Memory<byte> prefixBuffer;

        private bool prefixLoaded;
        private int prefixPosition;
        private int position;

        public CryptoPrefixStream(Stream stream, int cryptoBlockSize, CryptoStreamMode mode, bool removePrefix, bool leaveOpen)
            : base(stream, leaveOpen)
        {
            if (cryptoBlockSize < 16 || cryptoBlockSize % 8 != 0)
                throw new ArgumentException($"must be minimum of 16 and a multiple of 8.", nameof(cryptoBlockSize));

            this.cryptoStream = stream as CryptoStream;
            this.blockSizeBytes = cryptoBlockSize / 8;
            this.keySizeBytes = blockSizeBytes;
            this.mode = mode;
            this.removePrefix = removePrefix;

            this.prefixBufferOwner = ArrayPoolHelper<byte>.Rent(this.keySizeBytes);
            this.prefixBuffer = this.prefixBufferOwner.AsMemory().Slice(0, this.keySizeBytes);

            if (!removePrefix)
            {
                using (var rng = RandomNumberGenerator.Create())
                {
#if NETSTANDARD2_0
                    rng.GetBytes(keyBufferOwner, 0, keySizeBytes);
#else
                    rng.GetBytes(prefixBuffer.Span);
#endif
                }
            }

            this.prefixPosition = 0;
            this.position = 0;
        }

        protected override void Dispose(bool disposing)
        {
            if (prefixBufferOwner is not null)
            {
                ArrayPoolHelper<byte>.Return(prefixBufferOwner);
                prefixBufferOwner = null;
                prefixBuffer = null;
            }
            base.Dispose(disposing);
        }
#if !NETSTANDARD2_0
        public override ValueTask DisposeAsync()
        {
            if (prefixBufferOwner is not null)
            {
                ArrayPoolHelper<byte>.Return(prefixBufferOwner);
                prefixBufferOwner = null;
                prefixBuffer = null;
            }
            return base.DisposeAsync();
        }
#endif

        public void FlushFinalBlock()
        {
            if (cryptoStream is not null)
                cryptoStream.FlushFinalBlock();
        }

#if NET5_0_OR_GREATER
        public ValueTask FlushFinalBlockAsync(CancellationToken cancellationToken = default)
        {
            if (cryptoStream is not null)
                return cryptoStream.FlushFinalBlockAsync(cancellationToken);
            return ValueTask.CompletedTask;
        }
#endif

        public override bool CanRead => mode == CryptoStreamMode.Read;
        public override bool CanSeek => false;
        public override bool CanWrite => mode == CryptoStreamMode.Write;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => position; set => throw new NotSupportedException(); }

        public override long Seek(long offset, SeekOrigin origin) { throw new NotSupportedException(); }
        public override void SetLength(long value) { throw new NotSupportedException(); }

        protected override int InternalRead(Span<byte> buffer)
        {
#if NETSTANDARD2_0
            if (!CanRead || workingBufferOwner is null)
#else
            if (!CanRead)
#endif
                throw new InvalidOperationException($"Cannot read in {nameof(CryptoStreamMode)}.{mode}");

            var readTotal = 0;

            if (!prefixLoaded)
            {
                if (!removePrefix)
                {
                    var size = Math.Min(keySizeBytes - prefixPosition, buffer.Length);
                    prefixBuffer.Span.Slice(prefixPosition, size).CopyTo(buffer.Slice(0, size));
                    prefixPosition += size;
                    position += size;
                    if (prefixPosition < keySizeBytes)
                        return size;
                    readTotal += size;
                    prefixLoaded = true;
                    prefixPosition = 0;
                }
                else
                {
                    while (prefixPosition < keySizeBytes)
                    {
                        int prefixRead;

#if NETSTANDARD2_0
                        keyRead = stream.Read(keyBufferOwner, keyPosition, keySizeBytes - keyPosition);
#else
                        if (prefixPosition == 0)
                            prefixRead = stream.Read(prefixBuffer.Span);
                        else
                            prefixRead = stream.Read(prefixBuffer.Span[prefixPosition..keySizeBytes]);
#endif

                        if (prefixRead == 0)
                        {
                            if (prefixPosition > 0 && prefixPosition < keySizeBytes)
                                throw new InvalidOperationException("Stream ended before the prefix block was read");
                            break;
                        }
                        prefixPosition += prefixRead;
                    }
                    position += prefixPosition;
                    prefixLoaded = true;
                    prefixPosition = 0;
                }
            }

            var offsetFromKey = readTotal;
            while (readTotal < buffer.Length)
            {
                int read;

#if NETSTANDARD2_0
                var size = Math.Min(workingBufferOwner.Length - readTotal, buffer.Length - readTotal);
                read = stream.Read(workingBufferOwner, 0, size);
                workingBuffer.Span.Slice(0, read).CopyTo(buffer.Slice(readTotal, read));
#else
                read = stream.Read(buffer[readTotal..]);
#endif

                if (read == 0)
                    break;
                readTotal += read;
            }

            prefixPosition += readTotal - offsetFromKey;
            prefixPosition %= keySizeBytes;

            position += readTotal;

            return readTotal;
        }
        protected override async ValueTask<int> InternalReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
#if NETSTANDARD2_0
            if (!CanRead || workingBufferOwner is null)
#else
            if (!CanRead)
#endif
                throw new InvalidOperationException($"Cannot read in {nameof(CryptoStreamMode)}.{mode}");

            var readTotal = 0;

            if (!prefixLoaded)
            {
                if (!removePrefix)
                {
                    var size = Math.Min(keySizeBytes - prefixPosition, buffer.Length);
                    prefixBuffer.Slice(prefixPosition, size).CopyTo(buffer.Slice(0, size));
                    prefixPosition += size;
                    position += size;
                    if (prefixPosition < keySizeBytes)
                        return size;
                    readTotal += size;
                    prefixLoaded = true;
                    prefixPosition = 0;
                }
                else
                {
                    while (prefixPosition < keySizeBytes)
                    {
                        int prefixRead;

#if NETSTANDARD2_0
                        keyRead = await stream.ReadAsync(keyBufferOwner, keyPosition, keySizeBytes - keyPosition, cancellationToken);
#else
                        if (prefixPosition == 0)
                            prefixRead = await stream.ReadAsync(prefixBuffer, cancellationToken);
                        else
                            prefixRead = await stream.ReadAsync(prefixBuffer[prefixPosition..keySizeBytes], cancellationToken);
#endif

                        if (prefixRead == 0)
                        {
                            if (prefixPosition > 0 && prefixPosition < keySizeBytes)
                                throw new InvalidOperationException("Stream ended before the prefix block was read");
                            break;
                        }
                        prefixPosition += prefixRead;
                    }
                    position += prefixPosition;
                    prefixLoaded = true;
                    prefixPosition = 0;
                }
            }

            var offsetFromKey = readTotal;
            while (readTotal < buffer.Length)
            {
                int read;

#if NETSTANDARD2_0
                var size = Math.Min(workingBufferOwner.Length - readTotal, buffer.Length - readTotal);
                read = await stream.ReadAsync(workingBufferOwner, 0, size, cancellationToken);
                workingBuffer.Slice(0, read).CopyTo(buffer.Slice(readTotal, read));
#else
                read = await stream.ReadAsync(buffer[readTotal..], cancellationToken);
#endif

                if (read == 0)
                    break;
                readTotal += read;
            }

            prefixPosition += readTotal - offsetFromKey;
            prefixPosition %= keySizeBytes;

            position += readTotal;

            return readTotal;
        }

        protected override void InternalWrite(ReadOnlySpan<byte> buffer)
        {
            if (!CanWrite)
                throw new InvalidOperationException($"Cannot read in {nameof(CryptoStreamMode)}.{mode}");

            var readTotal = 0;

            if (!prefixLoaded)
            {
                if (!removePrefix)
                {
#if NETSTANDARD2_0
                    stream.Write(keyBufferOwner, 0, keySizeBytes);
#else
                    stream.Write(prefixBuffer.Span);
#endif
                    prefixLoaded = true;
                }
                else
                {
                    var size = Math.Min(keySizeBytes - prefixPosition, buffer.Length);
                    buffer.Slice(0, size).CopyTo(prefixBuffer.Span.Slice(prefixPosition, size));

                    prefixPosition += size;
                    if (prefixPosition < keySizeBytes)
                        return;
                    readTotal += size;
                    prefixLoaded = true;
                    prefixPosition = 0;
                }
            }

            while (readTotal < buffer.Length)
            {
                var read = buffer.Length - readTotal;
#if NETSTANDARD2_0
                stream.Write(buffer, readTotal, read);
#else
                stream.Write(buffer.Slice(readTotal, read));
#endif

                readTotal += read;
                position += readTotal;
            }
        }
        protected override async ValueTask InternalWriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (!CanWrite)
                throw new InvalidOperationException($"Cannot read in {nameof(CryptoStreamMode)}.{mode}");

            var readTotal = 0;

            if (!prefixLoaded)
            {
                if (!removePrefix)
                {
#if NETSTANDARD2_0
                    await stream.WriteAsync(keyBufferOwner, 0, keySizeBytes, cancellationToken);
#else
                    await stream.WriteAsync(prefixBuffer, cancellationToken);
#endif
                    prefixLoaded = true;
                }
                else
                {
                    var size = Math.Min(keySizeBytes - prefixPosition, buffer.Length);
                    buffer.Slice(0, size).CopyTo(prefixBuffer.Slice(prefixPosition, size));

                    prefixPosition += size;
                    if (prefixPosition < keySizeBytes)
                        return;
                    readTotal += size;
                    prefixLoaded = true;
                    prefixPosition = 0;
                }
            }

            while (readTotal < buffer.Length)
            {
                var read = buffer.Length - readTotal;

#if NETSTANDARD2_0
                await stream.WriteAsync(buffer, readTotal, read, cancellationToken);
#else
                await stream.WriteAsync(buffer.Slice(readTotal, read), cancellationToken);
#endif

                readTotal += read;
                position += readTotal;
            }
        }
    }
}