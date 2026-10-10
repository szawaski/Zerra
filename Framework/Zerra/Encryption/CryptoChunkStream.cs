// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Zerra.Buffers;
using Zerra.IO;

namespace Zerra.Encryption
{
    //Format: {header}{chunk}...{final chunk}
    //  header: {8 byte key check, the start of HMAC-SHA256 of a label with the key, so the key is picked without decrypting}{16 random bytes, except AES_CBC}
    //  chunk: {uint32 little endian: body length, high bit set on the final chunk}{body}
    //  AES_GCM: each message gets its own key, HMAC-SHA256 of the key and header, so a random nonce can't repeat under one key; the nonce is the chunk number; body: {ciphertext}{16 byte tag}, the length field is the associated data
    //  AES_CBC_HMAC body: {16 byte IV}{ciphertext}{32 byte HMAC-SHA256 of header, chunk number, length field, IV, and ciphertext}
    //  AES_CBC body: {16 byte IV}{ciphertext}, nothing is authenticated
    //The chunk number and final flag in the tag stop chunks being reordered, moved between messages, or cut off the end.
    internal sealed partial class CryptoChunkStream : StreamTransform
    {
        internal const int ChunkSize = 64 * 1024;
        private const int lengthSize = 4;
        private const uint finalFlag = 0x80000000;
        private const int blockSize = 16;
        private const int gcmTagSize = 16;
        private const int macSize = 32;
        internal const int KeyCheckSize = 8;
        private const int headerRandomSize = 16;
        private const int maxHeaderSize = KeyCheckSize + headerRandomSize;
        private const int maxBodySize = ChunkSize + blockSize + blockSize + macSize;

#if NETSTANDARD2_0
        private static readonly RandomNumberGenerator rng = RandomNumberGenerator.Create();
#endif
        private static readonly byte[] macKeyLabel = Encoding.ASCII.GetBytes("Zerra AES_CBC_HMAC");
        private static readonly byte[] keyCheckLabel = Encoding.ASCII.GetBytes("Zerra key check");

        internal static byte[] GetKeyCheck(byte[] key)
        {
            var check = new byte[KeyCheckSize];
            using (var derive = new HMACSHA256(key))
                Buffer.BlockCopy(derive.ComputeHash(keyCheckLabel), 0, check, 0, KeyCheckSize);
            return check;
        }

        private readonly SymmetricAlgorithmType algorithm;
        private readonly SymmetricKeyRing? ring;
        private readonly int headerSize;
        private Codec? codec;
        private readonly bool encrypt;
        private readonly CryptoStreamMode mode;

        private byte[]? inBufferOwner;
        private byte[]? outBufferOwner;
        private int inLength;
        private int outOffset;
        private int outLength;
        private int expected;
        private Phase phase;
        private bool finished;
        private long position;
        //how far into the buffers anything was written, only that is cleared when they go back to the pool
        private int used;

        private enum Phase : byte { Header, Length, Body }

        public CryptoChunkStream(Stream stream, SymmetricAlgorithmType algorithm, byte[] key, bool encrypt, CryptoStreamMode mode, bool leaveOpen)
            : base(stream, leaveOpen)
        {
            this.algorithm = algorithm;
            this.codec = new Codec(algorithm, key);
            this.headerSize = codec.HeaderSize;
            this.encrypt = encrypt;
            this.mode = mode;
            this.inBufferOwner = ArrayPoolHelper<byte>.Rent(lengthSize + maxBodySize);
            this.outBufferOwner = ArrayPoolHelper<byte>.Rent(maxHeaderSize + lengthSize + maxBodySize);
            this.phase = Phase.Header;
            this.expected = headerSize;
        }

        //the key comes from the ring when the first chunk is written or read, and stays for the whole stream
        public CryptoChunkStream(Stream stream, SymmetricAlgorithmType algorithm, SymmetricKeyRing ring, bool encrypt, CryptoStreamMode mode, bool leaveOpen)
            : base(stream, leaveOpen)
        {
            this.algorithm = algorithm;
            this.ring = ring;
            this.headerSize = Codec.GetHeaderSize(algorithm);
            this.encrypt = encrypt;
            this.mode = mode;
            this.inBufferOwner = ArrayPoolHelper<byte>.Rent(lengthSize + maxBodySize);
            this.outBufferOwner = ArrayPoolHelper<byte>.Rent(maxHeaderSize + lengthSize + maxBodySize);
            this.phase = Phase.Header;
            this.expected = headerSize;
        }

        public override bool CanRead => mode == CryptoStreamMode.Read;
        public override bool CanSeek => false;
        public override bool CanWrite => mode == CryptoStreamMode.Write;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => position; set => throw new NotSupportedException(); }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();

        public void FlushFinalBlock()
        {
            if (mode != CryptoStreamMode.Write || finished)
                return;
            if (encrypt)
            {
                codec ??= new Codec(algorithm, ring!.GetState().Current);
                var length = EncryptChunk(true);
                stream.Write(outBufferOwner!, 0, length);
                //like CryptoStream, the end is flushed through to the stream underneath
                stream.Flush();
            }
            else
            {
                throw new CryptographicException("Encrypted data ended before its final chunk");
            }
            finished = true;
        }

#if !NETSTANDARD2_0
        public async ValueTask FlushFinalBlockAsync(CancellationToken cancellationToken = default)
        {
            if (mode != CryptoStreamMode.Write || finished)
                return;
            if (encrypt)
            {
                codec ??= new Codec(algorithm, (await ring!.GetStateAsync()).Current);
                var length = EncryptChunk(true);
                await stream.WriteAsync(outBufferOwner.AsMemory(0, length), cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }
            else
            {
                throw new CryptographicException("Encrypted data ended before its final chunk");
            }
            finished = true;
        }
#endif

        protected override void Dispose(bool disposing)
        {
            if (inBufferOwner is not null)
            {
                //like CryptoStream, an encrypting writer that wasn't finished writes its end
                if (disposing && encrypt && mode == CryptoStreamMode.Write && !finished)
                    FlushFinalBlock();
                ArrayPoolHelper<byte>.Return(inBufferOwner, Math.Max(used, inLength));
                ArrayPoolHelper<byte>.Return(outBufferOwner!, used);
                inBufferOwner = null;
                outBufferOwner = null;
                codec?.Dispose();
            }
            base.Dispose(disposing);
        }

#if !NETSTANDARD2_0
        public override async ValueTask DisposeAsync()
        {
            if (inBufferOwner is not null)
            {
                if (encrypt && mode == CryptoStreamMode.Write && !finished)
                    await FlushFinalBlockAsync();
                ArrayPoolHelper<byte>.Return(inBufferOwner, Math.Max(used, inLength));
                ArrayPoolHelper<byte>.Return(outBufferOwner!, used);
                inBufferOwner = null;
                outBufferOwner = null;
                codec?.Dispose();
            }
            await base.DisposeAsync();
        }
#endif

        //encrypts the plain bytes waiting in the in buffer into the out buffer, with the header first on the first chunk
        private int EncryptChunk(bool final)
        {
            var offset = 0;
            if (phase == Phase.Header)
            {
                codec!.CreateHeader(outBufferOwner.AsSpan(0, headerSize));
                offset = headerSize;
                phase = Phase.Length;
            }
            var length = codec!.EncryptChunk(inBufferOwner.AsSpan(0, inLength), final, outBufferOwner.AsSpan(offset));
            inLength = 0;
            if (offset + length > used)
                used = offset + length;
            return offset + length;
        }

        protected override int InternalRead(Span<byte> buffer)
        {
            if (mode != CryptoStreamMode.Read)
                throw new InvalidOperationException($"Cannot read in {nameof(CryptoStreamMode)}.{mode}");

            while (outOffset == outLength)
            {
                if (finished)
                    return 0;
                if (encrypt)
                {
                    //fills a chunk from the source, a short read means its end
                    while (inLength < ChunkSize)
                    {
                        var read = stream.Read(inBufferOwner!, inLength, ChunkSize - inLength);
                        if (read == 0)
                        {
                            finished = true;
                            break;
                        }
                        inLength += read;
                    }
                    codec ??= new Codec(algorithm, ring!.GetState().Current);
                    outLength = EncryptChunk(finished);
                    outOffset = 0;
                }
                else
                {
                    if (phase == Phase.Header)
                    {
                        ReadExactly(headerSize, 0);
                        codec ??= new Codec(algorithm, ring!.SelectKey(inBufferOwner.AsSpan(0, KeyCheckSize)));
                        codec.ReadHeader(inBufferOwner.AsSpan(0, headerSize));
                        phase = Phase.Length;
                    }
                    ReadExactly(lengthSize, 0);
                    var lengthField = BinaryPrimitives.ReadUInt32LittleEndian(inBufferOwner.AsSpan(0, lengthSize));
                    var bodyLength = Codec.GetBodyLength(lengthField);
                    if (lengthSize + bodyLength > used)
                        used = lengthSize + bodyLength;
                    ReadExactly(bodyLength, lengthSize);
                    outLength = codec!.DecryptChunk(lengthField, inBufferOwner.AsSpan(lengthSize, bodyLength), outBufferOwner);
                    outOffset = 0;
                    finished = (lengthField & finalFlag) != 0;
                }
            }

            var size = Math.Min(outLength - outOffset, buffer.Length);
            outBufferOwner.AsSpan(outOffset, size).CopyTo(buffer);
            outOffset += size;
            position += size;
            return size;
        }

        //reads exactly the count into the in buffer at the offset, never past the end of the encrypted data
        private void ReadExactly(int count, int offset)
        {
            var total = 0;
            while (total < count)
            {
                var read = stream.Read(inBufferOwner!, offset + total, count - total);
                if (read == 0)
                    throw new CryptographicException("Encrypted data ended before its final chunk");
                total += read;
            }
        }

        protected override async ValueTask<int> InternalReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            if (mode != CryptoStreamMode.Read)
                throw new InvalidOperationException($"Cannot read in {nameof(CryptoStreamMode)}.{mode}");

            while (outOffset == outLength)
            {
                if (finished)
                    return 0;
                if (encrypt)
                {
                    while (inLength < ChunkSize)
                    {
#if NETSTANDARD2_0
                        var read = await stream.ReadAsync(inBufferOwner!, inLength, ChunkSize - inLength, cancellationToken);
#else
                        var read = await stream.ReadAsync(inBufferOwner.AsMemory(inLength, ChunkSize - inLength), cancellationToken);
#endif
                        if (read == 0)
                        {
                            finished = true;
                            break;
                        }
                        inLength += read;
                    }
                    codec ??= new Codec(algorithm, (await ring!.GetStateAsync()).Current);
                    outLength = EncryptChunk(finished);
                    outOffset = 0;
                }
                else
                {
                    if (phase == Phase.Header)
                    {
                        await ReadExactlyAsync(headerSize, 0, cancellationToken);
                        codec ??= new Codec(algorithm, await ring!.SelectKeyAsync(inBufferOwner.AsSpan(0, KeyCheckSize).ToArray()));
                        codec.ReadHeader(inBufferOwner.AsSpan(0, headerSize));
                        phase = Phase.Length;
                    }
                    await ReadExactlyAsync(lengthSize, 0, cancellationToken);
                    var lengthField = BinaryPrimitives.ReadUInt32LittleEndian(inBufferOwner.AsSpan(0, lengthSize));
                    var bodyLength = Codec.GetBodyLength(lengthField);
                    if (lengthSize + bodyLength > used)
                        used = lengthSize + bodyLength;
                    await ReadExactlyAsync(bodyLength, lengthSize, cancellationToken);
                    outLength = codec!.DecryptChunk(lengthField, inBufferOwner.AsSpan(lengthSize, bodyLength), outBufferOwner);
                    outOffset = 0;
                    finished = (lengthField & finalFlag) != 0;
                }
            }

            var size = Math.Min(outLength - outOffset, buffer.Length);
            outBufferOwner.AsSpan(outOffset, size).CopyTo(buffer.Span);
            outOffset += size;
            position += size;
            return size;
        }

        private async ValueTask ReadExactlyAsync(int count, int offset, CancellationToken cancellationToken)
        {
            var total = 0;
            while (total < count)
            {
#if NETSTANDARD2_0
                var read = await stream.ReadAsync(inBufferOwner!, offset + total, count - total, cancellationToken);
#else
                var read = await stream.ReadAsync(inBufferOwner.AsMemory(offset + total, count - total), cancellationToken);
#endif
                if (read == 0)
                    throw new CryptographicException("Encrypted data ended before its final chunk");
                total += read;
            }
        }

        protected override void InternalWrite(ReadOnlySpan<byte> buffer)
        {
            if (mode != CryptoStreamMode.Write)
                throw new InvalidOperationException($"Cannot write in {nameof(CryptoStreamMode)}.{mode}");
            position += buffer.Length;
            while (buffer.Length > 0)
            {
                if (finished)
                    throw new CryptographicException("Data written after the final chunk");
                if (encrypt)
                {
                    //a full chunk is only written once more data comes, the last one is written by FlushFinalBlock
                    if (inLength == ChunkSize)
                    {
                        codec ??= new Codec(algorithm, ring!.GetState().Current);
                        var length = EncryptChunk(false);
                        stream.Write(outBufferOwner!, 0, length);
                    }
                    var size = Math.Min(ChunkSize - inLength, buffer.Length);
                    buffer.Slice(0, size).CopyTo(inBufferOwner.AsSpan(inLength));
                    inLength += size;
                    buffer = buffer.Slice(size);
                }
                else
                {
                    var size = Math.Min(expected - inLength, buffer.Length);
                    buffer.Slice(0, size).CopyTo(inBufferOwner.AsSpan(inLength));
                    inLength += size;
                    buffer = buffer.Slice(size);
                    while (inLength == expected)
                    {
                        var plainLength = DecryptWritten();
                        if (plainLength > 0)
                            stream.Write(outBufferOwner!, 0, plainLength);
                    }
                }
            }
        }

        protected override async ValueTask InternalWriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
        {
            if (mode != CryptoStreamMode.Write)
                throw new InvalidOperationException($"Cannot write in {nameof(CryptoStreamMode)}.{mode}");
            position += buffer.Length;
            while (buffer.Length > 0)
            {
                if (finished)
                    throw new CryptographicException("Data written after the final chunk");
                if (encrypt)
                {
                    if (inLength == ChunkSize)
                    {
                        codec ??= new Codec(algorithm, (await ring!.GetStateAsync()).Current);
                        var length = EncryptChunk(false);
#if NETSTANDARD2_0
                        await stream.WriteAsync(outBufferOwner!, 0, length, cancellationToken);
#else
                        await stream.WriteAsync(outBufferOwner.AsMemory(0, length), cancellationToken);
#endif
                    }
                    var size = Math.Min(ChunkSize - inLength, buffer.Length);
                    buffer.Span.Slice(0, size).CopyTo(inBufferOwner.AsSpan(inLength));
                    inLength += size;
                    buffer = buffer.Slice(size);
                }
                else
                {
                    var size = Math.Min(expected - inLength, buffer.Length);
                    buffer.Span.Slice(0, size).CopyTo(inBufferOwner.AsSpan(inLength));
                    inLength += size;
                    buffer = buffer.Slice(size);
                    while (inLength == expected)
                    {
                        if (phase == Phase.Header && codec is null)
                            codec = new Codec(algorithm, await ring!.SelectKeyAsync(inBufferOwner.AsSpan(0, KeyCheckSize).ToArray()));
                        var plainLength = DecryptWritten();
                        if (plainLength > 0)
                        {
#if NETSTANDARD2_0
                            await stream.WriteAsync(outBufferOwner!, 0, plainLength, cancellationToken);
#else
                            await stream.WriteAsync(outBufferOwner.AsMemory(0, plainLength), cancellationToken);
#endif
                        }
                    }
                }
            }
        }

        //called once the in buffer holds what the phase expected, moves to the next phase and returns the plain bytes ready in the out buffer
        private int DecryptWritten()
        {
            if (finished)
                throw new CryptographicException("Data written after the final chunk");
            switch (phase)
            {
                case Phase.Header:
                    codec ??= new Codec(algorithm, ring!.SelectKey(inBufferOwner.AsSpan(0, KeyCheckSize)));
                    codec.ReadHeader(inBufferOwner.AsSpan(0, headerSize));
                    phase = Phase.Length;
                    inLength = 0;
                    expected = lengthSize;
                    return 0;
                case Phase.Length:
                    var bodyLength = Codec.GetBodyLength(BinaryPrimitives.ReadUInt32LittleEndian(inBufferOwner.AsSpan(0, lengthSize)));
                    phase = Phase.Body;
                    expected = lengthSize + bodyLength;
                    if (expected > used)
                        used = expected;
                    return 0;
                default:
                    var lengthField = BinaryPrimitives.ReadUInt32LittleEndian(inBufferOwner.AsSpan(0, lengthSize));
                    var plainLength = codec!.DecryptChunk(lengthField, inBufferOwner.AsSpan(lengthSize, expected - lengthSize), outBufferOwner);
                    finished = (lengthField & finalFlag) != 0;
                    phase = Phase.Length;
                    inLength = 0;
                    expected = lengthSize;
                    return plainLength;
            }
        }

        //the byte array and span calls skip the stream
        public static byte[] Encrypt(SymmetricAlgorithmType algorithm, byte[] key, ReadOnlySpan<byte> plain)
        {
            using var codec = new Codec(algorithm, key);
            var chunks = plain.Length == 0 ? 1 : (plain.Length + ChunkSize - 1) / ChunkSize;
            var lastLength = plain.Length - (chunks - 1) * ChunkSize;
            var total = codec.HeaderSize + (chunks - 1) * (lengthSize + codec.GetBodySize(ChunkSize)) + lengthSize + codec.GetBodySize(lastLength);
            var output = new byte[total];
            codec.CreateHeader(output.AsSpan(0, codec.HeaderSize));
            var offset = codec.HeaderSize;
            for (var i = 0; i < chunks; i++)
            {
                var chunk = i < chunks - 1 ? plain.Slice(i * ChunkSize, ChunkSize) : plain.Slice(i * ChunkSize);
                offset += codec.EncryptChunk(chunk, i == chunks - 1, output.AsSpan(offset));
            }
            return output;
        }

        public static byte[] Decrypt(SymmetricAlgorithmType algorithm, byte[] key, ReadOnlySpan<byte> encrypted)
        {
            using var codec = new Codec(algorithm, key);
            if (encrypted.Length < codec.HeaderSize)
                throw new CryptographicException("Encrypted data ended before its final chunk");
            codec.ReadHeader(encrypted.Slice(0, codec.HeaderSize));
            var offset = codec.HeaderSize;

#if !NETSTANDARD2_0
            //GCM's plain length is known from the chunk lengths, so it decrypts straight into the result
            if (algorithm == SymmetricAlgorithmType.AES_GCM)
            {
                var plainLength = 0;
                var scan = offset;
                for (; ; )
                {
                    if (encrypted.Length - scan < lengthSize)
                        throw new CryptographicException("Encrypted data ended before its final chunk");
                    var lengthField = BinaryPrimitives.ReadUInt32LittleEndian(encrypted.Slice(scan, lengthSize));
                    var bodyLength = Codec.GetBodyLength(lengthField);
                    scan += lengthSize;
                    if (encrypted.Length - scan < bodyLength)
                        throw new CryptographicException("Encrypted data ended before its final chunk");
                    if (bodyLength < gcmTagSize)
                        throw new CryptographicException("Encrypted chunk is too short");
                    plainLength += bodyLength - gcmTagSize;
                    scan += bodyLength;
                    if ((lengthField & finalFlag) != 0)
                        break;
                }
                if (scan != encrypted.Length)
                    throw new CryptographicException("Data after the final chunk");

                var plain = new byte[plainLength];
                var plainOffset = 0;
                while (offset < encrypted.Length)
                {
                    var lengthField = BinaryPrimitives.ReadUInt32LittleEndian(encrypted.Slice(offset, lengthSize));
                    var bodyLength = Codec.GetBodyLength(lengthField);
                    offset += lengthSize;
                    plainOffset += codec.DecryptChunk(lengthField, encrypted.Slice(offset, bodyLength), plain.AsSpan(plainOffset));
                    offset += bodyLength;
                }
                return plain;
            }
#endif

            //the plain bytes are always fewer than the encrypted ones, and each chunk decrypts in place after the ones before it
            var plainBufferOwner = ArrayPoolHelper<byte>.Rent(encrypted.Length);
            try
            {
                var written = 0;
                for (; ; )
                {
                    if (encrypted.Length - offset < lengthSize)
                        throw new CryptographicException("Encrypted data ended before its final chunk");
                    var lengthField = BinaryPrimitives.ReadUInt32LittleEndian(encrypted.Slice(offset, lengthSize));
                    var bodyLength = Codec.GetBodyLength(lengthField);
                    offset += lengthSize;
                    if (encrypted.Length - offset < bodyLength)
                        throw new CryptographicException("Encrypted data ended before its final chunk");
                    written += codec.DecryptChunk(lengthField, encrypted.Slice(offset, bodyLength), plainBufferOwner.AsSpan(written));
                    offset += bodyLength;
                    if ((lengthField & finalFlag) != 0)
                        break;
                }
                if (offset != encrypted.Length)
                    throw new CryptographicException("Data after the final chunk");
                return plainBufferOwner.AsSpan(0, written).ToArray();
            }
            finally
            {
                ArrayPoolHelper<byte>.Return(plainBufferOwner, encrypted.Length);
            }
        }
    }
}
