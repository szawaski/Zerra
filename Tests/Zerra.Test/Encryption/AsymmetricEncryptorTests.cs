// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Security.Cryptography;
using System.Text;
using Xunit;
using Zerra.Encryption;

namespace Zerra.Test.Encryption
{
    public class AsymmetricEncryptorTests
    {
        private static readonly AsymmetricKeyPair keys = AsymmetricEncryptor.GenerateKey();

        public static TheoryData<AsymmetricAlgorithmType> Algorithms => [AsymmetricAlgorithmType.RSA_OAEP_256_A256GCM, AsymmetricAlgorithmType.RSA_OAEP_A256CBC_HS512];

        [Theory]
        [MemberData(nameof(Algorithms))]
        public void RoundTrip(AsymmetricAlgorithmType algorithm)
        {
            Assert.StartsWith("-----BEGIN PUBLIC KEY-----", keys.PublicKey);
            Assert.StartsWith("-----BEGIN PRIVATE KEY-----", keys.PrivateKey);

            const string plain = "secret é 😀";
            var encrypted = AsymmetricEncryptor.Encrypt(algorithm, keys.PublicKey, plain);
            Assert.Equal(5, encrypted.Split('.').Length);
            Assert.NotEqual(encrypted, AsymmetricEncryptor.Encrypt(algorithm, keys.PublicKey, plain));
            Assert.Equal(plain, AsymmetricEncryptor.Decrypt(keys.PrivateKey!, encrypted));

            //any size, unlike RSA alone
            var large = Enumerable.Range(0, 1_000_000).Select(x => (byte)x).ToArray();
            Assert.Equal(large, AsymmetricEncryptor.DecryptBytes(keys.PrivateKey!, AsymmetricEncryptor.Encrypt(algorithm, keys.PublicKey, large)));
            Assert.Empty(AsymmetricEncryptor.DecryptBytes(keys.PrivateKey!, AsymmetricEncryptor.Encrypt(algorithm, keys.PublicKey, Array.Empty<byte>())));

            //a private key PEM also holds the public key
            Assert.Equal(plain, AsymmetricEncryptor.Decrypt(keys.PrivateKey!, AsymmetricEncryptor.Encrypt(algorithm, keys.PrivateKey!, plain)));
        }

        [Theory]
        [MemberData(nameof(Algorithms))]
        public void WrongKey_Throws(AsymmetricAlgorithmType algorithm)
        {
            var other = AsymmetricEncryptor.GenerateKey();
            var encrypted = AsymmetricEncryptor.Encrypt(algorithm, keys.PublicKey, "secret");
            _ = Assert.ThrowsAny<CryptographicException>(() => AsymmetricEncryptor.Decrypt(other.PrivateKey!, encrypted));
            _ = Assert.ThrowsAny<CryptographicException>(() => AsymmetricEncryptor.Decrypt(keys.PublicKey, encrypted));
        }

        [Theory]
        [MemberData(nameof(Algorithms))]
        public void Changed_Throws(AsymmetricAlgorithmType algorithm)
        {
            var encrypted = AsymmetricEncryptor.Encrypt(algorithm, keys.PublicKey, "secret data");
            var parts = encrypted.Split('.');
            for (var i = 1; i < parts.Length; i++)
            {
                var changed = (string[])parts.Clone();
                var bytes = Base64UrlEncoder.FromBase64UrlString(changed[i]);
                bytes[0] ^= 0x01;
                changed[i] = Base64UrlEncoder.ToBase64UrlString(bytes);
                _ = Assert.ThrowsAny<CryptographicException>(() => AsymmetricEncryptor.Decrypt(keys.PrivateKey!, String.Join(".", changed)));
            }

            //a header the tag wasn't made with, even an equivalent one
            var reordered = (string[])parts.Clone();
            reordered[0] = Base64UrlEncoder.ToBase64UrlString(Encoding.UTF8.GetBytes(algorithm == AsymmetricAlgorithmType.RSA_OAEP_256_A256GCM ? "{\"enc\":\"A256GCM\",\"alg\":\"RSA-OAEP-256\"}" : "{\"enc\":\"A256CBC-HS512\",\"alg\":\"RSA-OAEP\"}"));
            _ = Assert.ThrowsAny<CryptographicException>(() => AsymmetricEncryptor.Decrypt(keys.PrivateKey!, String.Join(".", reordered)));

            _ = Assert.ThrowsAny<CryptographicException>(() => AsymmetricEncryptor.Decrypt(keys.PrivateKey!, "a.b"));
            _ = Assert.ThrowsAny<CryptographicException>(() => AsymmetricEncryptor.Decrypt(keys.PrivateKey!, "!.!.!.!.!"));
        }

        [Theory]
        [InlineData("{\"alg\":\"RSA1_5\",\"enc\":\"A256GCM\"}")]
        [InlineData("{\"alg\":\"RSA-OAEP\",\"enc\":\"A256GCM\"}")]
        [InlineData("{\"alg\":\"RSA-OAEP-256\",\"enc\":\"A128CBC-HS256\"}")]
        [InlineData("{\"alg\":\"RSA-OAEP-256\",\"enc\":\"A256GCM\",\"zip\":\"DEF\"}")]
        [InlineData("{\"alg\":\"RSA-OAEP-256\",\"enc\":\"A256GCM\",\"crit\":[\"exp\"]}")]
        [InlineData("[]")]
        [InlineData("{\"alg\":5,\"enc\":\"A256GCM\"}")]
        public void UnsupportedHeader_Throws(string header)
        {
            var jwe = CreateJwe(keys.PublicKey, header, Encoding.UTF8.GetBytes("secret"));
            _ = Assert.ThrowsAny<CryptographicException>(() => AsymmetricEncryptor.Decrypt(keys.PrivateKey!, jwe));
        }

        [Theory]
        [MemberData(nameof(Algorithms))]
        public void SmallKeys_Throw(AsymmetricAlgorithmType algorithm)
        {
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => AsymmetricEncryptor.GenerateKey(1024));
            using var rsa = RSA.Create(1024);
            _ = Assert.ThrowsAny<CryptographicException>(() => AsymmetricEncryptor.Encrypt(algorithm, rsa.ExportSubjectPublicKeyInfoPem(), "secret"));
            _ = Assert.Throws<ArgumentNullException>(() => AsymmetricEncryptor.Encrypt(algorithm, null!, "secret"));
            _ = Assert.Throws<ArgumentNullException>(() => AsymmetricEncryptor.Encrypt(algorithm, keys.PublicKey, (string)null!));
            _ = Assert.Throws<ArgumentNullException>(() => AsymmetricEncryptor.Decrypt(keys.PrivateKey!, null!));
        }

        //JWE built directly from the RFC 7516 steps, the way another JOSE library would, with its header in another order and an extra member
        private static string CreateJwe(string publicKey, string header, byte[] plain)
        {
            using var rsa = RSA.Create();
            rsa.ImportFromPem(publicKey);
            var contentKey = RandomNumberGenerator.GetBytes(32);
            var iv = RandomNumberGenerator.GetBytes(12);
            var cipher = new byte[plain.Length];
            var tag = new byte[16];
            var encodedHeader = Base64UrlEncoder.ToBase64UrlString(Encoding.UTF8.GetBytes(header));
            using (var gcm = new AesGcm(contentKey, 16))
                gcm.Encrypt(iv, plain, cipher, tag, Encoding.ASCII.GetBytes(encodedHeader));
            return String.Join(".", encodedHeader, Base64UrlEncoder.ToBase64UrlString(rsa.Encrypt(contentKey, RSAEncryptionPadding.OaepSHA256)), Base64UrlEncoder.ToBase64UrlString(iv), Base64UrlEncoder.ToBase64UrlString(cipher), Base64UrlEncoder.ToBase64UrlString(tag));
        }

        [Fact]
        public void Standard_ReadsOtherJwe()
        {
            var plain = Encoding.UTF8.GetBytes("from another library");
            var jwe = CreateJwe(keys.PublicKey, "{\"enc\":\"A256GCM\",\"kid\":\"key-1\",\"alg\":\"RSA-OAEP-256\"}", plain);
            Assert.Equal(plain, AsymmetricEncryptor.DecryptBytes(keys.PrivateKey!, jwe));
        }

        [Fact]
        public void Standard_OtherJweReaderReadsIt()
        {
            const AsymmetricAlgorithmType algorithm = AsymmetricAlgorithmType.RSA_OAEP_256_A256GCM;
            var plain = Encoding.UTF8.GetBytes("for another library");
            var parts = AsymmetricEncryptor.Encrypt(algorithm, keys.PublicKey, plain).Split('.');

            Assert.Equal("{\"alg\":\"RSA-OAEP-256\",\"enc\":\"A256GCM\"}", Encoding.UTF8.GetString(Base64UrlEncoder.FromBase64UrlString(parts[0])));
            using var rsa = RSA.Create();
            rsa.ImportFromPem(keys.PrivateKey);
            var contentKey = rsa.Decrypt(Base64UrlEncoder.FromBase64UrlString(parts[1]), RSAEncryptionPadding.OaepSHA256);
            var cipher = Base64UrlEncoder.FromBase64UrlString(parts[3]);
            var result = new byte[cipher.Length];
            using (var gcm = new AesGcm(contentKey, 16))
                gcm.Decrypt(Base64UrlEncoder.FromBase64UrlString(parts[2]), cipher, Base64UrlEncoder.FromBase64UrlString(parts[4]), result, Encoding.ASCII.GetBytes(parts[0]));
            Assert.Equal(plain, result);
        }

        [Fact]
        public void Standard_CbcHs512_OtherJweReaderReadsIt()
        {
            //RFC 7518 5.2.2 read step by step: RSA-OAEP key, MAC key then AES key, HMAC-SHA512 over header, IV, ciphertext, and the header length in bits
            var plain = Encoding.UTF8.GetBytes("for another library");
            var parts = AsymmetricEncryptor.Encrypt(AsymmetricAlgorithmType.RSA_OAEP_A256CBC_HS512, keys.PublicKey, plain).Split('.');

            Assert.Equal("{\"alg\":\"RSA-OAEP\",\"enc\":\"A256CBC-HS512\"}", Encoding.UTF8.GetString(Base64UrlEncoder.FromBase64UrlString(parts[0])));
            using var rsa = RSA.Create();
            rsa.ImportFromPem(keys.PrivateKey);
            var contentKey = rsa.Decrypt(Base64UrlEncoder.FromBase64UrlString(parts[1]), RSAEncryptionPadding.OaepSHA1);
            Assert.Equal(64, contentKey.Length);
            var iv = Base64UrlEncoder.FromBase64UrlString(parts[2]);
            var cipher = Base64UrlEncoder.FromBase64UrlString(parts[3]);
            var aad = Encoding.ASCII.GetBytes(parts[0]);
            var al = BitConverter.GetBytes((ulong)aad.Length * 8);
            Array.Reverse(al);
            var mac = HMACSHA512.HashData(contentKey[..32], aad.Concat(iv).Concat(cipher).Concat(al).ToArray());
            Assert.Equal(mac[..32], Base64UrlEncoder.FromBase64UrlString(parts[4]));
            using var aes = Aes.Create();
            aes.Key = contentKey[32..];
            Assert.Equal(plain, aes.DecryptCbc(cipher, iv, PaddingMode.PKCS7));
        }

    }
}
