// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Encryption
{
    public interface IEncryptor
    {
        byte[] Encrypt(byte[] bytes);
        byte[] Decrypt(byte[] bytes);

#if !NETSTANDARD2_0
        Span<byte> Encrypt(ReadOnlySpan<byte> bytes);
        Span<byte> Decrypt(ReadOnlySpan<byte> bytes);
#endif

        CryptoFlushStream Encrypt(Stream stream, bool write);
        CryptoFlushStream Decrypt(Stream stream, bool write);
    }
}
