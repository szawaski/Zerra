// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Encryption
{
    /// <summary>
    /// Defines encryption and decryption operations for byte arrays and streams.
    /// </summary>
    /// <remarks>
    /// Provides methods to encrypt and decrypt data in various formats: byte arrays, spans, and streams.
    /// Implementations handle the actual encryption algorithm and key management.
    /// </remarks>
    public interface IEncryptor
    {
        /// <summary>
        /// Encrypts a byte array.
        /// </summary>
        /// <param name="bytes">The plaintext bytes to encrypt.</param>
        /// <returns>The encrypted bytes.</returns>
        byte[] Encrypt(byte[] bytes);

        /// <summary>
        /// Decrypts a byte array.
        /// </summary>
        /// <param name="bytes">The encrypted bytes to decrypt.</param>
        /// <returns>The decrypted plaintext bytes.</returns>
        byte[] Decrypt(byte[] bytes);

#if !NETSTANDARD2_0
        /// <summary>
        /// Encrypts a byte span.
        /// </summary>
        /// <remarks>
        /// Available on .NET 5.0 and later (not available on .NET Standard 2.0).
        /// </remarks>
        /// <param name="bytes">The plaintext bytes to encrypt.</param>
        /// <returns>A span containing the encrypted bytes.</returns>
        Span<byte> Encrypt(ReadOnlySpan<byte> bytes);

        /// <summary>
        /// Decrypts a byte span.
        /// </summary>
        /// <remarks>
        /// Available on .NET 5.0 and later (not available on .NET Standard 2.0).
        /// </remarks>
        /// <param name="bytes">The encrypted bytes to decrypt.</param>
        /// <returns>A span containing the decrypted plaintext bytes.</returns>
        Span<byte> Decrypt(ReadOnlySpan<byte> bytes);
#endif

        /// <summary>
        /// Creates an encryption or decryption stream wrapper.
        /// </summary>
        /// <remarks>
        /// Provides a stream-based interface for encrypting or decrypting data incrementally.
        /// Useful for large data or streaming scenarios.
        /// </remarks>
        /// <param name="stream">The underlying stream to wrap.</param>
        /// <param name="write">True to create an encryption stream for writing; false to create a decryption stream for reading.</param>
        /// <returns>A crypto stream that encrypts or decrypts data transparently.</returns>
        CryptoFlushStream Encrypt(Stream stream, bool write);

        /// <summary>
        /// Creates a decryption stream wrapper.
        /// </summary>
        /// <remarks>
        /// Provides a stream-based interface for decrypting data incrementally.
        /// Useful for large data or streaming scenarios.
        /// </remarks>
        /// <param name="stream">The underlying stream to wrap.</param>
        /// <param name="write">True to create a decryption stream for writing; false to create a decryption stream for reading.</param>
        /// <returns>A crypto stream that decrypts data transparently.</returns>
        CryptoFlushStream Decrypt(Stream stream, bool write);
    }
}
