// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Zerra.Buffers;
using Zerra.IO;

namespace Zerra.Encryption
{
    public class CryptoShiftStream : StreamTransform
    {
        private const int bufferSize = 8 * 1024;

        private readonly CryptoStream? cryptoStream;
        private readonly int blockSizeBytes;
        private readonly int keySizeBytes;
        private readonly CryptoStreamMode mode;
        private readonly bool deshift;

        private byte[]? keyBufferOwner;
        private byte[]? workingBufferOwner;

        private Memory<byte> keyBuffer;
        private Memory<byte> workingBuffer;

        private bool keyLoaded;
        private int keyPosition;
        private int position;

        public CryptoShiftStream(Stream stream, int cryptoBlockSize, CryptoStreamMode mode, bool deshift, bool leaveOpen) 
            : base(stream, leaveOpen)
        {
            if (cryptoBlockSize < 16 || cryptoBlockSize % 8 != 0)
                throw new ArgumentException($"must be minimum of 16 and a multiple of 8.", nameof(cryptoBlockSize));

            this.cryptoStream = stream as CryptoStream;
            this.blockSizeBytes = cryptoBlockSize / 8;
            this.keySizeBytes = blockSizeBytes;
            this.mode = mode;
            this.deshift = deshift;

            this.keyBufferOwner = ArrayPoolHelper<byte>.Rent(this.keySizeBytes);
            this.keyBuffer = this.keyBufferOwner.AsMemory().Slice(0, this.keySizeBytes);
#if NETSTANDARD2_0

            this.workingBufferOwner = ArrayPoolHelper<byte>.Rent(bufferSize);
            this.workingBuffer = this.workingBufferOwner.AsMemory();
#else
            if (mode == CryptoStreamMode.Write)
            {
                this.workingBufferOwner = ArrayPoolHelper<byte>.Rent(bufferSize);
                this.workingBuffer = this.workingBufferOwner.AsMemory();
            }
            else
            {
                this.workingBufferOwner = null;
                this.workingBuffer = null;
            }
#endif

            if (!deshift)
            {
                using (var rng = RandomNumberGenerator.Create())
                {
#if NETSTANDARD2_0
                    rng.GetBytes(keyBufferOwner, 0, keySizeBytes);
#else
                    rng.GetBytes(keyBuffer.Span);
#endif
                }
            }

            this.keyPosition = 0;
            this.position = 0;
        }

        protected override void Dispose(bool disposing)
        {
            if (keyBufferOwner != null)
            {
                Array.Clear(keyBufferOwner, 0, keyBufferOwner.Length);
                ArrayPoolHelper<byte>.Return(keyBufferOwner);
                keyBufferOwner = null;
                keyBuffer = null;
            }
            if (workingBufferOwner != null)
            {
                Array.Clear(workingBufferOwner, 0, workingBufferOwner.Length);
                ArrayPoolHelper<byte>.Return(workingBufferOwner);
                workingBufferOwner = null;
                workingBuffer = null;
            }
            base.Dispose(disposing);
        }
#if !NETSTANDARD2_0
        public override ValueTask DisposeAsync()
        {
            if (keyBufferOwner != null)
            {
                Array.Clear(keyBufferOwner, 0, keyBufferOwner.Length);
                ArrayPoolHelper<byte>.Return(keyBufferOwner);
                keyBufferOwner = null;
                keyBuffer = null;
            }
            if (workingBufferOwner != null)
            {
                Array.Clear(workingBufferOwner, 0, workingBufferOwner.Length);
                ArrayPoolHelper<byte>.Return(workingBufferOwner);
                workingBufferOwner = null;
                workingBuffer = null;
            }
            return base.DisposeAsync();
        }
#endif

        public void FlushFinalBlock()
        {
            if (cryptoStream != null)
                cryptoStream.FlushFinalBlock();
        }

#if NET5_0_OR_GREATER
        public ValueTask FlushFinalBlockAsync()
        {
            if (cryptoStream != null)
                return cryptoStream.FlushFinalBlockAsync();
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
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
#if NETSTANDARD2_0
            if (!CanRead || workingBufferOwner == null)
#else
            if (!CanRead)
#endif
                throw new InvalidOperationException($"Cannot read in {nameof(CryptoStreamMode)}.{mode}");

            var readTotal = 0;

            if (!keyLoaded)
            {
                if (!deshift)
                {
                    var size = Math.Min(keySizeBytes - keyPosition, buffer.Length);
                    keyBuffer.Span.Slice(keyPosition, size).CopyTo(buffer.Slice(0, size));
                    keyPosition += size;
                    position += size;
                    if (keyPosition < keySizeBytes)
                        return size;
                    readTotal += size;
                    keyLoaded = true;
                    keyPosition = 0;
                }
                else
                {
                    while (keyPosition < keySizeBytes)
                    {
                        int keyRead;

#if NETSTANDARD2_0
                        keyRead = stream.Read(keyBufferOwner, keyPosition, keySizeBytes - keyPosition);
#else
                        if (keyPosition == 0)
                            keyRead = stream.Read(keyBuffer.Span);
                        else
                            keyRead = stream.Read(keyBuffer.Span[keyPosition..keySizeBytes]);
#endif

                        if (keyRead == 0)
                        {
                            if (keyPosition > 0 && keyPosition < keySizeBytes)
                                throw new InvalidOperationException("Stream ended before the key block was read");
                            break;
                        }
                        keyPosition += keyRead;
                    }
                    position += keyPosition;
                    keyLoaded = true;
                    keyPosition = 0;
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

            Shift(deshift, buffer, offsetFromKey, readTotal - offsetFromKey, keyBuffer.Span);

            position += readTotal;

            return readTotal;
        }
        protected override async ValueTask<int> InternalReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
#if NETSTANDARD2_0
            if (!CanRead || workingBufferOwner == null)
#else
            if (!CanRead)
#endif
                throw new InvalidOperationException($"Cannot read in {nameof(CryptoStreamMode)}.{mode}");

            var readTotal = 0;

            if (!keyLoaded)
            {
                if (!deshift)
                {
                    var size = Math.Min(keySizeBytes - keyPosition, buffer.Length);
                    keyBuffer.Slice(keyPosition, size).CopyTo(buffer.Slice(0, size));
                    keyPosition += size;
                    position += size;
                    if (keyPosition < keySizeBytes)
                        return size;
                    readTotal += size;
                    keyLoaded = true;
                    keyPosition = 0;
                }
                else
                {
                    while (keyPosition < keySizeBytes)
                    {
                        int keyRead;

#if NETSTANDARD2_0
                        keyRead = await stream.ReadAsync(keyBufferOwner, keyPosition, keySizeBytes - keyPosition, cancellationToken);
#else
                        if (keyPosition == 0)
                            keyRead = await stream.ReadAsync(keyBuffer, cancellationToken);
                        else
                            keyRead = await stream.ReadAsync(keyBuffer[keyPosition..keySizeBytes], cancellationToken);
#endif

                        if (keyRead == 0)
                        {
                            if (keyPosition > 0 && keyPosition < keySizeBytes)
                                throw new InvalidOperationException("Stream ended before the key block was read");
                            break;
                        }
                        keyPosition += keyRead;
                    }
                    position += keyPosition;
                    keyLoaded = true;
                    keyPosition = 0;
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

            Shift(deshift, buffer.Span, offsetFromKey, readTotal - offsetFromKey, keyBuffer.Span);

            position += readTotal;

            return readTotal;
        }

        protected override void InternalWrite(ReadOnlySpan<byte> buffer)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (!CanWrite)
                throw new InvalidOperationException($"Cannot read in {nameof(CryptoStreamMode)}.{mode}");

            var readTotal = 0;

            if (!keyLoaded)
            {
                if (!deshift)
                {
#if NETSTANDARD2_0
                    stream.Write(keyBufferOwner, 0, keySizeBytes);
#else
                    stream.Write(keyBuffer.Span);
#endif
                    keyLoaded = true;
                }
                else
                {
                    var size = Math.Min(keySizeBytes - keyPosition, buffer.Length);
                    buffer.Slice(0, size).CopyTo(keyBuffer.Span.Slice(keyPosition, size));

                    keyPosition += size;
                    if (keyPosition < keySizeBytes)
                        return;
                    readTotal += size;
                    keyLoaded = true;
                    keyPosition = 0;
                }
            }

            while (readTotal < buffer.Length)
            {
                var read = Math.Min(buffer.Length - readTotal, workingBuffer.Length);
                buffer.Slice(readTotal, read).CopyTo(workingBuffer.Span.Slice(0, read));

                Shift(deshift, workingBuffer.Span, 0, read, keyBuffer.Span);

#if NETSTANDARD2_0
                stream.Write(workingBufferOwner, 0, read);
#else
                stream.Write(workingBuffer.Span.Slice(0, read));
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

            if (!keyLoaded)
            {
                if (!deshift)
                {
#if NETSTANDARD2_0
                    await stream.WriteAsync(keyBufferOwner, 0, keySizeBytes, cancellationToken);
#else
                    await stream.WriteAsync(keyBuffer, cancellationToken);
#endif
                    keyLoaded = true;
                }
                else
                {
                    var size = Math.Min(keySizeBytes - keyPosition, buffer.Length);
                    buffer.Slice(0, size).CopyTo(keyBuffer.Slice(keyPosition, size));

                    keyPosition += size;
                    if (keyPosition < keySizeBytes)
                        return;
                    readTotal += size;
                    keyLoaded = true;
                    keyPosition = 0;
                }
            }

            while (readTotal < buffer.Length)
            {
                var read = Math.Min(buffer.Length - readTotal, workingBuffer.Length);
                buffer.Slice(readTotal, read).CopyTo(workingBuffer.Slice(0, read));

                Shift(deshift, workingBuffer.Span, 0, read, keyBuffer.Span);

#if NETSTANDARD2_0
                await stream.WriteAsync(workingBufferOwner, 0, read, cancellationToken);
#else
                await stream.WriteAsync(workingBuffer.Slice(0, read), cancellationToken);
#endif

                readTotal += read;
                position += readTotal;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void Shift(bool deshift, Span<byte> buffer, int offset, int count, Span<byte> key)
        {
            fixed (byte* pBuffer = &buffer[offset], pKey = key)
            {
                for (var i = 0; i < count; i++)
                {
                    if (!deshift)
                        pBuffer[i] = (byte)(pBuffer[i] + pKey[keyPosition]);
                    else
                        pBuffer[i] = (byte)(pBuffer[i] - pKey[keyPosition]);

                    keyPosition++;
                    if (keyPosition == keySizeBytes)
                        keyPosition = 0;
                }
            }
        }
    }
}