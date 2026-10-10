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
    //  chunk: {uint32 little endian: body length, high bit set on the final chunk}{body}
    //  AES_GCM header: 16 random bytes, each message gets its own key, HMAC-SHA256 of the key and header, so a random nonce can't repeat under one key; the nonce is the chunk number; body: {ciphertext}{16 byte tag}, the length field is the associated data
    //  AES_CBC_HMAC header: 16 random bytes; body: {16 byte IV}{ciphertext}{32 byte HMAC-SHA256 of header, chunk number, length field, IV, and ciphertext}
    //  AES_CBC no header; body: {16 byte IV}{ciphertext}, nothing is authenticated
    //The chunk number and final flag in the tag stop chunks being reordered, moved between messages, or cut off the end.
    internal sealed class CryptoChunkStream : StreamTransform
    {
        internal const int ChunkSize = 64 * 1024;
        private const int lengthSize = 4;
        private const uint finalFlag = 0x80000000;
        private const int blockSize = 16;
        private const int gcmTagSize = 16;
        private const int macSize = 32;
        private const int maxHeaderSize = 16;
        private const int maxBodySize = ChunkSize + blockSize + blockSize + macSize;

#if NETSTANDARD2_0
        private static readonly RandomNumberGenerator rng = RandomNumberGenerator.Create();
#endif
        private static readonly byte[] macKeyLabel = Encoding.ASCII.GetBytes("Zerra AES_CBC_HMAC");

        private readonly Codec codec;
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

        private enum Phase : byte { Header, Length, Body }

        public CryptoChunkStream(Stream stream, SymmetricAlgorithmType algorithm, byte[] key, bool encrypt, CryptoStreamMode mode, bool leaveOpen)
            : base(stream, leaveOpen)
        {
            this.codec = new Codec(algorithm, key);
            this.encrypt = encrypt;
            this.mode = mode;
            this.inBufferOwner = ArrayPoolHelper<byte>.Rent(lengthSize + maxBodySize);
            this.outBufferOwner = ArrayPoolHelper<byte>.Rent(maxHeaderSize + lengthSize + maxBodySize);
            this.phase = codec.HeaderSize > 0 ? Phase.Header : Phase.Length;
            this.expected = codec.HeaderSize > 0 ? codec.HeaderSize : lengthSize;
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
                ArrayPoolHelper<byte>.Return(inBufferOwner);
                ArrayPoolHelper<byte>.Return(outBufferOwner!);
                inBufferOwner = null;
                outBufferOwner = null;
                codec.Dispose();
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
                ArrayPoolHelper<byte>.Return(inBufferOwner);
                ArrayPoolHelper<byte>.Return(outBufferOwner!);
                inBufferOwner = null;
                outBufferOwner = null;
                codec.Dispose();
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
                codec.CreateHeader(outBufferOwner.AsSpan(0, codec.HeaderSize));
                offset = codec.HeaderSize;
                phase = Phase.Length;
            }
            var length = codec.EncryptChunk(inBufferOwner.AsSpan(0, inLength), final, outBufferOwner.AsSpan(offset));
            inLength = 0;
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
                    outLength = EncryptChunk(finished);
                    outOffset = 0;
                }
                else
                {
                    if (phase == Phase.Header)
                    {
                        ReadExactly(codec.HeaderSize, 0);
                        codec.ReadHeader(inBufferOwner.AsSpan(0, codec.HeaderSize));
                        phase = Phase.Length;
                    }
                    ReadExactly(lengthSize, 0);
                    var lengthField = BinaryPrimitives.ReadUInt32LittleEndian(inBufferOwner.AsSpan(0, lengthSize));
                    var bodyLength = Codec.GetBodyLength(lengthField);
                    ReadExactly(bodyLength, lengthSize);
                    outLength = codec.DecryptChunk(lengthField, inBufferOwner.AsSpan(lengthSize, bodyLength), outBufferOwner);
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
                    outLength = EncryptChunk(finished);
                    outOffset = 0;
                }
                else
                {
                    if (phase == Phase.Header)
                    {
                        await ReadExactlyAsync(codec.HeaderSize, 0, cancellationToken);
                        codec.ReadHeader(inBufferOwner.AsSpan(0, codec.HeaderSize));
                        phase = Phase.Length;
                    }
                    await ReadExactlyAsync(lengthSize, 0, cancellationToken);
                    var lengthField = BinaryPrimitives.ReadUInt32LittleEndian(inBufferOwner.AsSpan(0, lengthSize));
                    var bodyLength = Codec.GetBodyLength(lengthField);
                    await ReadExactlyAsync(bodyLength, lengthSize, cancellationToken);
                    outLength = codec.DecryptChunk(lengthField, inBufferOwner.AsSpan(lengthSize, bodyLength), outBufferOwner);
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
                    codec.ReadHeader(inBufferOwner.AsSpan(0, codec.HeaderSize));
                    phase = Phase.Length;
                    inLength = 0;
                    expected = lengthSize;
                    return 0;
                case Phase.Length:
                    var bodyLength = Codec.GetBodyLength(BinaryPrimitives.ReadUInt32LittleEndian(inBufferOwner.AsSpan(0, lengthSize)));
                    phase = Phase.Body;
                    expected = lengthSize + bodyLength;
                    return 0;
                default:
                    var lengthField = BinaryPrimitives.ReadUInt32LittleEndian(inBufferOwner.AsSpan(0, lengthSize));
                    var plainLength = codec.DecryptChunk(lengthField, inBufferOwner.AsSpan(lengthSize, expected - lengthSize), outBufferOwner);
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
                ArrayPoolHelper<byte>.Return(plainBufferOwner);
            }
        }

        private sealed class Codec : IDisposable
        {
            private readonly SymmetricAlgorithmType algorithm;
            private readonly byte[] header;
            private uint counter;
#if !NETSTANDARD2_0
            private readonly byte[]? gcmKey;
            private AesGcm? gcm;
#endif
            private readonly Aes? aes;
            private readonly IncrementalHash? hmac;

            public int HeaderSize { get; }

            public Codec(SymmetricAlgorithmType algorithm, byte[] key)
            {
                this.algorithm = algorithm;
                switch (algorithm)
                {
                    case SymmetricAlgorithmType.AES_GCM:
#if NETSTANDARD2_0
                        throw new PlatformNotSupportedException($"{nameof(SymmetricAlgorithmType.AES_GCM)} needs .NET Core 3.0 or later, use {nameof(SymmetricAlgorithmType.AES_CBC_HMAC)} on this platform");
#else
                        gcmKey = key;
                        HeaderSize = 16;
                        break;
#endif
                    case SymmetricAlgorithmType.AES_CBC_HMAC:
                        aes = Aes.Create();
                        aes.Key = key;
                        byte[] macKey;
                        using (var derive = new HMACSHA256(key))
                            macKey = derive.ComputeHash(macKeyLabel);
                        hmac = IncrementalHash.CreateHMAC(HashAlgorithmName.SHA256, macKey);
                        HeaderSize = 16;
                        break;
                    case SymmetricAlgorithmType.AES_CBC:
                        aes = Aes.Create();
                        aes.Key = key;
                        HeaderSize = 0;
                        break;
                    default:
                        throw new NotSupportedException($"{algorithm} is not a chunked algorithm");
                }
                header = new byte[HeaderSize];
            }

            public void Dispose()
            {
#if !NETSTANDARD2_0
                gcm?.Dispose();
#endif
                aes?.Dispose();
                hmac?.Dispose();
            }

            public static int GetBodyLength(uint lengthField)
            {
                var bodyLength = (int)(lengthField & ~finalFlag);
                if (bodyLength > maxBodySize)
                    throw new CryptographicException("Encrypted chunk is larger than allowed");
                return bodyLength;
            }

            public int GetBodySize(int plainLength) => algorithm switch
            {
                SymmetricAlgorithmType.AES_GCM => plainLength + gcmTagSize,
                SymmetricAlgorithmType.AES_CBC_HMAC => blockSize + (plainLength / blockSize + 1) * blockSize + macSize,
                _ => blockSize + (plainLength / blockSize + 1) * blockSize,
            };

            public void CreateHeader(Span<byte> destination)
            {
                if (HeaderSize == 0)
                    return;
                Fill(header);
                header.CopyTo(destination);
                CreateMessageKey();
            }

            public void ReadHeader(ReadOnlySpan<byte> source)
            {
                if (HeaderSize == 0)
                    return;
                source.CopyTo(header);
                CreateMessageKey();
            }

            //writes {length}{body} and returns its size
            public int EncryptChunk(ReadOnlySpan<byte> plain, bool final, Span<byte> destination)
            {
                var bodyLength = GetBodySize(plain.Length);
                var lengthField = (uint)bodyLength | (final ? finalFlag : 0);
                BinaryPrimitives.WriteUInt32LittleEndian(destination, lengthField);
                var body = destination.Slice(lengthSize, bodyLength);

                switch (algorithm)
                {
#if !NETSTANDARD2_0
                    case SymmetricAlgorithmType.AES_GCM:
                        {
                            Span<byte> nonce = stackalloc byte[12];
                            BinaryPrimitives.WriteUInt32BigEndian(nonce.Slice(8), NextCounter());
                            gcm!.Encrypt(nonce, plain, body.Slice(0, plain.Length), body.Slice(plain.Length, gcmTagSize), destination.Slice(0, lengthSize));
                            break;
                        }
#endif
                    case SymmetricAlgorithmType.AES_CBC_HMAC:
                        {
                            var iv = body.Slice(0, blockSize);
                            Fill(iv);
                            var cipherLength = bodyLength - blockSize - macSize;
                            EncryptCbc(plain, iv, body.Slice(blockSize, cipherLength));
                            ComputeMac(NextCounter(), destination.Slice(0, lengthSize), body.Slice(0, blockSize + cipherLength), body.Slice(blockSize + cipherLength, macSize));
                            break;
                        }
                    default:
                        {
                            var iv = body.Slice(0, blockSize);
                            Fill(iv);
                            EncryptCbc(plain, iv, body.Slice(blockSize));
                            break;
                        }
                }
                return lengthSize + bodyLength;
            }

            //decrypts the body into the destination and returns the plain length
            public int DecryptChunk(uint lengthField, ReadOnlySpan<byte> body, Span<byte> destination)
            {
                Span<byte> lengthBytes = stackalloc byte[lengthSize];
                BinaryPrimitives.WriteUInt32LittleEndian(lengthBytes, lengthField);

                switch (algorithm)
                {
#if !NETSTANDARD2_0
                    case SymmetricAlgorithmType.AES_GCM:
                        {
                            if (body.Length < gcmTagSize)
                                throw new CryptographicException("Encrypted chunk is too short");
                            var plainLength = body.Length - gcmTagSize;
                            Span<byte> nonce = stackalloc byte[12];
                            BinaryPrimitives.WriteUInt32BigEndian(nonce.Slice(8), NextCounter());
                            gcm!.Decrypt(nonce, body.Slice(0, plainLength), body.Slice(plainLength, gcmTagSize), destination.Slice(0, plainLength), lengthBytes);
                            return plainLength;
                        }
#endif
                    case SymmetricAlgorithmType.AES_CBC_HMAC:
                        {
                            var cipherLength = body.Length - blockSize - macSize;
                            if (cipherLength < blockSize || cipherLength % blockSize != 0)
                                throw new CryptographicException("Encrypted chunk is the wrong size");
                            Span<byte> mac = stackalloc byte[macSize];
                            ComputeMac(NextCounter(), lengthBytes, body.Slice(0, blockSize + cipherLength), mac);
                            if (!FixedTimeEquals(mac, body.Slice(blockSize + cipherLength, macSize)))
                                throw new CryptographicException("Encrypted data was changed or the key is wrong");
                            return DecryptCbc(body.Slice(blockSize, cipherLength), body.Slice(0, blockSize), destination);
                        }
                    default:
                        {
                            var cipherLength = body.Length - blockSize;
                            if (cipherLength < blockSize || cipherLength % blockSize != 0)
                                throw new CryptographicException("Encrypted chunk is the wrong size");
                            return DecryptCbc(body.Slice(blockSize), body.Slice(0, blockSize), destination);
                        }
                }
            }

            private void CreateMessageKey()
            {
#if !NETSTANDARD2_0
                if (gcmKey is null)
                    return;
                //the message key is the same size as the key
                Span<byte> messageKey = stackalloc byte[32];
                _ = HMACSHA256.HashData(gcmKey, header, messageKey);
                gcm?.Dispose();
                gcm = new AesGcm(messageKey.Slice(0, gcmKey.Length), gcmTagSize);
                CryptographicOperations.ZeroMemory(messageKey);
#endif
            }

            private uint NextCounter()
            {
                if (counter == uint.MaxValue)
                    throw new CryptographicException("Too many encrypted chunks");
                return counter++;
            }

            private void ComputeMac(uint chunkNumber, ReadOnlySpan<byte> lengthBytes, ReadOnlySpan<byte> ivAndCipher, Span<byte> destination)
            {
                Span<byte> counterBytes = stackalloc byte[4];
                BinaryPrimitives.WriteUInt32BigEndian(counterBytes, chunkNumber);
#if NETSTANDARD2_0
                hmac!.AppendData(header);
                hmac.AppendData(counterBytes.ToArray());
                hmac.AppendData(lengthBytes.ToArray());
                hmac.AppendData(ivAndCipher.ToArray());
                hmac.GetHashAndReset().CopyTo(destination);
#else
                hmac!.AppendData(header);
                hmac.AppendData(counterBytes);
                hmac.AppendData(lengthBytes);
                hmac.AppendData(ivAndCipher);
                _ = hmac.GetHashAndReset(destination);
#endif
            }

            private void EncryptCbc(ReadOnlySpan<byte> plain, ReadOnlySpan<byte> iv, Span<byte> destination)
            {
#if NETSTANDARD2_0
                using (var transform = aes!.CreateEncryptor(aes.Key, iv.ToArray()))
                    transform.TransformFinalBlock(plain.ToArray(), 0, plain.Length).CopyTo(destination);
#else
                _ = aes!.EncryptCbc(plain, iv, destination, PaddingMode.PKCS7);
#endif
            }

            private int DecryptCbc(ReadOnlySpan<byte> cipher, ReadOnlySpan<byte> iv, Span<byte> destination)
            {
#if NETSTANDARD2_0
                using (var transform = aes!.CreateDecryptor(aes.Key, iv.ToArray()))
                {
                    var plain = transform.TransformFinalBlock(cipher.ToArray(), 0, cipher.Length);
                    plain.CopyTo(destination);
                    return plain.Length;
                }
#else
                return aes!.DecryptCbc(cipher, iv, destination, PaddingMode.PKCS7);
#endif
            }

            private static void Fill(Span<byte> destination)
            {
#if NETSTANDARD2_0
                var bytes = new byte[destination.Length];
                lock (rng)
                    rng.GetBytes(bytes);
                bytes.CopyTo(destination);
#else
                RandomNumberGenerator.Fill(destination);
#endif
            }

            private static bool FixedTimeEquals(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right)
            {
#if NETSTANDARD2_0
                if (left.Length != right.Length)
                    return false;
                var difference = 0;
                for (var i = 0; i < left.Length; i++)
                    difference |= left[i] ^ right[i];
                return difference == 0;
#else
                return CryptographicOperations.FixedTimeEquals(left, right);
#endif
            }
        }
    }
}
