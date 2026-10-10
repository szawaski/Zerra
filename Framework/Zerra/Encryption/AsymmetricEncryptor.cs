// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Zerra.Encryption
{
    /// <summary>
    /// Encrypts data that only the holder of a private key can decrypt, as JSON Web Encryption (RFC 7516) in compact form.
    /// The data is encrypted under a random key, and that key with RSA, so the data can be any size and any JOSE library can decrypt it. Keys are PEM.
    /// </summary>
    public static class AsymmetricEncryptor
    {
        private const int defaultKeySize = 2048;
        private const int minimumKeySize = 2048;
        private const int gcmIVSize = 12;
        private const int gcmTagSize = 16;
        private const int cbcIVSize = 16;
        private const int cbcHmacTagSize = 32;
        private const string gcmKeyAlgorithm = "RSA-OAEP-256";
        private const string gcmContentAlgorithm = "A256GCM";
        private const string cbcKeyAlgorithm = "RSA-OAEP";
        private const string cbcContentAlgorithm = "A256CBC-HS512";
        private static readonly string gcmHeader = Base64UrlEncoder.ToBase64UrlString(Encoding.UTF8.GetBytes($"{{\"alg\":\"{gcmKeyAlgorithm}\",\"enc\":\"{gcmContentAlgorithm}\"}}"));
        private static readonly string cbcHeader = Base64UrlEncoder.ToBase64UrlString(Encoding.UTF8.GetBytes($"{{\"alg\":\"{cbcKeyAlgorithm}\",\"enc\":\"{cbcContentAlgorithm}\"}}"));
#if NETSTANDARD2_0
        private static readonly RandomNumberGenerator rng = RandomNumberGenerator.Create();
#endif

        /// <summary>
        /// Generates a new RSA key pair.
        /// </summary>
        /// <param name="keySize">The key size in bits, at least 2048.</param>
        /// <returns>The public key as SubjectPublicKeyInfo PEM and the private key as PKCS#8 PEM.</returns>
        public static AsymmetricKeyPair GenerateKey(int keySize = defaultKeySize)
        {
            if (keySize < minimumKeySize)
                throw new ArgumentOutOfRangeException(nameof(keySize), $"RSA keys must be at least {minimumKeySize} bits");

#if NETSTANDARD2_0
            var rsa = RSA.Create();
            try
            {
                //.NET Framework's RSACryptoServiceProvider ignores a key size set after it's created
                if (rsa is RSACryptoServiceProvider)
                {
                    rsa.Dispose();
                    rsa = new RSACryptoServiceProvider(keySize);
                }
                else
                {
                    rsa.KeySize = keySize;
                }
                var parameters = rsa.ExportParameters(true);
                return new AsymmetricKeyPair(RsaPem.ExportPublicKey(parameters), RsaPem.ExportPrivateKey(parameters));
            }
            finally
            {
                rsa.Dispose();
            }
#else
            using var rsa = RSA.Create(keySize);
            return new AsymmetricKeyPair(rsa.ExportSubjectPublicKeyInfoPem(), rsa.ExportPkcs8PrivateKeyPem());
#endif
        }

        /// <summary>
        /// Encrypts text for the holder of the private key.
        /// </summary>
        /// <param name="algorithm">The JWE algorithms.</param>
        /// <param name="publicKey">The RSA public key as PEM.</param>
        /// <param name="plainData">The text to encrypt.</param>
        /// <returns>The JWE in compact form.</returns>
        public static string Encrypt(AsymmetricAlgorithmType algorithm, string publicKey, string plainData)
        {
            if (plainData is null)
                throw new ArgumentNullException(nameof(plainData));
            return Encrypt(algorithm, publicKey, Encoding.UTF8.GetBytes(plainData));
        }

        /// <summary>
        /// Encrypts data for the holder of the private key.
        /// </summary>
        /// <param name="algorithm">The JWE algorithms.</param>
        /// <param name="publicKey">The RSA public key as PEM.</param>
        /// <param name="plainBytes">The data to encrypt.</param>
        /// <returns>The JWE in compact form.</returns>
        public static string Encrypt(AsymmetricAlgorithmType algorithm, string publicKey, byte[] plainBytes)
        {
            if (publicKey is null)
                throw new ArgumentNullException(nameof(publicKey));
            if (plainBytes is null)
                throw new ArgumentNullException(nameof(plainBytes));

            using var rsa = LoadKey(publicKey);
            if (rsa.KeySize < minimumKeySize)
                throw new CryptographicException($"RSA keys must be at least {minimumKeySize} bits");

            switch (algorithm)
            {
                case AsymmetricAlgorithmType.RSA_OAEP_256_A256GCM:
                    {
#if NETSTANDARD2_0
                        throw new PlatformNotSupportedException($"{nameof(AsymmetricAlgorithmType.RSA_OAEP_256_A256GCM)} needs .NET Core 3.0 or later, use {nameof(AsymmetricAlgorithmType.RSA_OAEP_A256CBC_HS512)} on this platform");
#else
                        var contentKey = RandomNumberGenerator.GetBytes(32);
                        try
                        {
                            var iv = RandomNumberGenerator.GetBytes(gcmIVSize);
                            var cipher = new byte[plainBytes.Length];
                            var tag = new byte[gcmTagSize];
                            var encryptedKey = rsa.Encrypt(contentKey, RSAEncryptionPadding.OaepSHA256);
                            //the associated data is the encoded header, so the header can't be changed either
                            using (var gcm = new AesGcm(contentKey, gcmTagSize))
                                gcm.Encrypt(iv, plainBytes, cipher, tag, Encoding.ASCII.GetBytes(gcmHeader));
                            return Join(gcmHeader, encryptedKey, iv, cipher, tag);
                        }
                        finally
                        {
                            CryptographicOperations.ZeroMemory(contentKey);
                        }
#endif
                    }
                case AsymmetricAlgorithmType.RSA_OAEP_A256CBC_HS512:
                    {
                        //RFC 7518 5.2: the first half of the key is for the MAC, the second for AES
                        var contentKey = RandomBytes(64);
                        try
                        {
                            var iv = RandomBytes(cbcIVSize);
                            var cipher = EncryptCbc(contentKey, iv, plainBytes);
                            var tag = ComputeCbcTag(contentKey, Encoding.ASCII.GetBytes(cbcHeader), iv, cipher);
                            var encryptedKey = rsa.Encrypt(contentKey, RSAEncryptionPadding.OaepSHA1);
                            return Join(cbcHeader, encryptedKey, iv, cipher, tag);
                        }
                        finally
                        {
                            Array.Clear(contentKey, 0, contentKey.Length);
                        }
                    }
                default:
                    throw new NotSupportedException($"{nameof(AsymmetricAlgorithmType)} {algorithm} is not supported");
            }
        }

        /// <summary>
        /// Decrypts text encrypted for this private key, with either <see cref="AsymmetricAlgorithmType"/>, which the JWE header says.
        /// </summary>
        /// <param name="privateKey">The RSA private key as PEM.</param>
        /// <param name="encryptedData">The JWE in compact form.</param>
        /// <returns>The decrypted text.</returns>
        public static string Decrypt(string privateKey, string encryptedData)
            => Encoding.UTF8.GetString(DecryptBytes(privateKey, encryptedData));

        /// <summary>
        /// Decrypts data encrypted for this private key, with either <see cref="AsymmetricAlgorithmType"/>, which the JWE header says.
        /// </summary>
        /// <param name="privateKey">The RSA private key as PEM.</param>
        /// <param name="encryptedData">The JWE in compact form.</param>
        /// <returns>The decrypted data.</returns>
        public static byte[] DecryptBytes(string privateKey, string encryptedData)
        {
            if (privateKey is null)
                throw new ArgumentNullException(nameof(privateKey));
            if (encryptedData is null)
                throw new ArgumentNullException(nameof(encryptedData));

            var parts = encryptedData.Split('.');
            if (parts.Length != 5)
                throw new CryptographicException("Not a JWE in compact form");

            AsymmetricAlgorithmType algorithm;
            byte[] encryptedKey, iv, cipher, tag;
            try
            {
                algorithm = ReadHeader(Base64UrlEncoder.FromBase64UrlString(parts[0]));
                encryptedKey = Base64UrlEncoder.FromBase64UrlString(parts[1]);
                iv = Base64UrlEncoder.FromBase64UrlString(parts[2]);
                cipher = Base64UrlEncoder.FromBase64UrlString(parts[3]);
                tag = Base64UrlEncoder.FromBase64UrlString(parts[4]);
            }
            catch (FormatException ex)
            {
                throw new CryptographicException("Not a JWE in compact form", ex);
            }
            var associatedData = Encoding.ASCII.GetBytes(parts[0]);

            using var rsa = LoadKey(privateKey);
            switch (algorithm)
            {
                case AsymmetricAlgorithmType.RSA_OAEP_256_A256GCM:
                    {
#if NETSTANDARD2_0
                        throw new PlatformNotSupportedException($"{nameof(AsymmetricAlgorithmType.RSA_OAEP_256_A256GCM)} needs .NET Core 3.0 or later");
#else
                        if (iv.Length != gcmIVSize || tag.Length != gcmTagSize)
                            throw new CryptographicException("Not a JWE in compact form");
                        var contentKey = rsa.Decrypt(encryptedKey, RSAEncryptionPadding.OaepSHA256);
                        try
                        {
                            if (contentKey.Length != 32)
                                throw new CryptographicException("Encrypted data was changed or the key is wrong");
                            var plain = new byte[cipher.Length];
                            using (var gcm = new AesGcm(contentKey, gcmTagSize))
                                gcm.Decrypt(iv, cipher, tag, plain, associatedData);
                            return plain;
                        }
                        finally
                        {
                            CryptographicOperations.ZeroMemory(contentKey);
                        }
#endif
                    }
                default:
                    {
                        if (iv.Length != cbcIVSize || tag.Length != cbcHmacTagSize || cipher.Length == 0 || cipher.Length % 16 != 0)
                            throw new CryptographicException("Not a JWE in compact form");
                        var contentKey = rsa.Decrypt(encryptedKey, RSAEncryptionPadding.OaepSHA1);
                        try
                        {
                            if (contentKey.Length != 64)
                                throw new CryptographicException("Encrypted data was changed or the key is wrong");
                            //checked before decrypting
                            if (!FixedTimeEquals(ComputeCbcTag(contentKey, associatedData, iv, cipher), tag))
                                throw new CryptographicException("Encrypted data was changed or the key is wrong");
                            return DecryptCbc(contentKey, iv, cipher);
                        }
                        finally
                        {
                            Array.Clear(contentKey, 0, contentKey.Length);
                        }
                    }
            }
        }

        private static RSA LoadKey(string pem)
        {
            var rsa = RSA.Create();
            try
            {
#if NETSTANDARD2_0
                rsa.ImportParameters(RsaPem.Import(pem));
#else
                rsa.ImportFromPem(pem);
#endif
                return rsa;
            }
            catch
            {
                rsa.Dispose();
                throw;
            }
        }

        private static string Join(string header, byte[] encryptedKey, byte[] iv, byte[] cipher, byte[] tag)
            => String.Join(".", header, Base64UrlEncoder.ToBase64UrlString(encryptedKey), Base64UrlEncoder.ToBase64UrlString(iv), Base64UrlEncoder.ToBase64UrlString(cipher), Base64UrlEncoder.ToBase64UrlString(tag));

        private static byte[] RandomBytes(int length)
        {
            var bytes = new byte[length];
#if NETSTANDARD2_0
            lock (rng)
                rng.GetBytes(bytes);
#else
            RandomNumberGenerator.Fill(bytes);
#endif
            return bytes;
        }

        private static byte[] EncryptCbc(byte[] contentKey, byte[] iv, byte[] plain)
        {
            using var aes = Aes.Create();
            aes.Key = contentKey.AsSpan(32, 32).ToArray();
#if NETSTANDARD2_0
            aes.IV = iv;
            using (var transform = aes.CreateEncryptor())
                return transform.TransformFinalBlock(plain, 0, plain.Length);
#else
            return aes.EncryptCbc(plain, iv, PaddingMode.PKCS7);
#endif
        }

        private static byte[] DecryptCbc(byte[] contentKey, byte[] iv, byte[] cipher)
        {
            using var aes = Aes.Create();
            aes.Key = contentKey.AsSpan(32, 32).ToArray();
#if NETSTANDARD2_0
            aes.IV = iv;
            using (var transform = aes.CreateDecryptor())
                return transform.TransformFinalBlock(cipher, 0, cipher.Length);
#else
            return aes.DecryptCbc(cipher, iv, PaddingMode.PKCS7);
#endif
        }

        //RFC 7518 5.2.2.1: HMAC-SHA512 of the associated data, IV, ciphertext, and the associated data's length in bits as 64 bits big endian, the first half is the tag
        private static byte[] ComputeCbcTag(byte[] contentKey, byte[] associatedData, byte[] iv, byte[] cipher)
        {
            var associatedLength = new byte[8];
            var bits = (ulong)associatedData.Length * 8;
            for (var i = 0; i < 8; i++)
                associatedLength[7 - i] = (byte)(bits >> (8 * i));

            using var hmac = new HMACSHA512(contentKey.AsSpan(0, 32).ToArray());
            _ = hmac.TransformBlock(associatedData, 0, associatedData.Length, null, 0);
            _ = hmac.TransformBlock(iv, 0, iv.Length, null, 0);
            _ = hmac.TransformBlock(cipher, 0, cipher.Length, null, 0);
            _ = hmac.TransformFinalBlock(associatedLength, 0, associatedLength.Length);
            var tag = new byte[cbcHmacTagSize];
            Buffer.BlockCopy(hmac.Hash!, 0, tag, 0, tag.Length);
            return tag;
        }

        private static bool FixedTimeEquals(byte[] left, byte[] right)
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

        //other JOSE libraries can write the header differently, so it's read rather than compared, and only the two algorithm pairs are accepted
        private static AsymmetricAlgorithmType ReadHeader(byte[] header)
        {
            string? alg = null;
            string? enc = null;
            try
            {
                var reader = new Utf8JsonReader(header);
                if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
                    throw new CryptographicException("Invalid JWE header");
                while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
                {
                    var name = reader.GetString();
                    _ = reader.Read();
                    switch (name)
                    {
                        case "alg":
                            alg = reader.GetString();
                            break;
                        case "enc":
                            enc = reader.GetString();
                            break;
                        case "zip":
                        case "crit":
                            throw new CryptographicException($"JWE header {name} is not supported");
                        default:
                            reader.Skip();
                            break;
                    }
                }
            }
            catch (Exception ex) when (ex is JsonException || ex is InvalidOperationException)
            {
                throw new CryptographicException("Invalid JWE header", ex);
            }
            if (alg == gcmKeyAlgorithm && enc == gcmContentAlgorithm)
                return AsymmetricAlgorithmType.RSA_OAEP_256_A256GCM;
            if (alg == cbcKeyAlgorithm && enc == cbcContentAlgorithm)
                return AsymmetricAlgorithmType.RSA_OAEP_A256CBC_HS512;
            throw new CryptographicException($"Only {gcmKeyAlgorithm} with {gcmContentAlgorithm}, or {cbcKeyAlgorithm} with {cbcContentAlgorithm}, is supported");
        }
    }
}
