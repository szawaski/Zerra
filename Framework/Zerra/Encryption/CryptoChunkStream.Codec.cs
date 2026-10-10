// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Buffers.Binary;
using System.Security.Cryptography;

namespace Zerra.Encryption
{
    internal sealed partial class CryptoChunkStream
    {
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
                        HeaderSize = KeyCheckSize + headerRandomSize;
                        break;
#endif
                    case SymmetricAlgorithmType.AES_CBC_HMAC:
                        aes = Aes.Create();
                        aes.Key = key;
                        byte[] macKey;
                        using (var derive = new HMACSHA256(key))
                            macKey = derive.ComputeHash(macKeyLabel);
                        hmac = IncrementalHash.CreateHMAC(HashAlgorithmName.SHA256, macKey);
                        HeaderSize = KeyCheckSize + headerRandomSize;
                        break;
                    case SymmetricAlgorithmType.AES_CBC:
                        aes = Aes.Create();
                        aes.Key = key;
                        HeaderSize = KeyCheckSize;
                        break;
                    default:
                        throw new NotSupportedException($"{algorithm} is not a chunked algorithm");
                }
                header = new byte[HeaderSize];
                GetKeyCheck(key).CopyTo(header, 0);
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

            public static int GetHeaderSize(SymmetricAlgorithmType algorithm) => algorithm switch
            {
#if NETSTANDARD2_0
                SymmetricAlgorithmType.AES_GCM => throw new PlatformNotSupportedException($"{nameof(SymmetricAlgorithmType.AES_GCM)} needs .NET Core 3.0 or later, use {nameof(SymmetricAlgorithmType.AES_CBC_HMAC)} on this platform"),
#else
                SymmetricAlgorithmType.AES_GCM => KeyCheckSize + headerRandomSize,
#endif
                SymmetricAlgorithmType.AES_CBC_HMAC => KeyCheckSize + headerRandomSize,
                SymmetricAlgorithmType.AES_CBC => KeyCheckSize,
                _ => throw new NotSupportedException($"{algorithm} is not a chunked algorithm"),
            };

            //{key check}{random bytes}, AES_CBC has only the key check
            public void CreateHeader(Span<byte> destination)
            {
                if (algorithm != SymmetricAlgorithmType.AES_CBC)
                {
                    Fill(header.AsSpan(KeyCheckSize));
                    CreateMessageKey();
                }
                header.CopyTo(destination);
            }

            public void ReadHeader(ReadOnlySpan<byte> source)
            {
                if (!FixedTimeEquals(source.Slice(0, KeyCheckSize), header.AsSpan(0, KeyCheckSize)))
                    throw new KeyMismatchException();
                if (algorithm == SymmetricAlgorithmType.AES_CBC)
                    return;
                source.Slice(KeyCheckSize).CopyTo(header.AsSpan(KeyCheckSize));
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
