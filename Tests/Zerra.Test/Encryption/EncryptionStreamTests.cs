// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using System.Security.Cryptography;
using Zerra.Encryption;

namespace Zerra.Test.Encryption
{
    public class EncryptionStreamTests
    {
        [Fact]
        public void CryptoPrefixStreamRead()
        {
            const int blockSize = 256;
            var test = GetTestBytes();

            byte[] result;
            using (var shiftmsin = new MemoryStream(test))
            using (var shiftmsout = new MemoryStream())
            using (var shiftstream = new CryptoPrefixStream(shiftmsin, blockSize, CryptoStreamMode.Read, false, false))
            {
                shiftstream.CopyTo(shiftmsout);
                result = shiftmsout.ToArray();
            }

            Assert.True(result.Length - blockSize / 8 == test.Length);

            byte[] unshiftresult;
            using (var unshiftmsin = new MemoryStream(result))
            using (var unshiftmsout = new MemoryStream())
            using (var unshift = new CryptoPrefixStream(unshiftmsin, blockSize, CryptoStreamMode.Read, true, false))
            {
                unshift.CopyTo(unshiftmsout);
                unshiftresult = unshiftmsout.ToArray();
            }

            Assert.Equal(test.Length, unshiftresult.Length);
            for (var i = 0; i < test.Length; i++)
                Assert.Equal(test[i], unshiftresult[i]);
        }

        [Fact]
        public void CryptoPrefixStreamWrite()
        {
            const int blockSize = 256;
            var test = GetTestBytes();

            byte[] result;
            using (var shiftmsout = new MemoryStream())
            using (var shift = new CryptoPrefixStream(shiftmsout, blockSize, CryptoStreamMode.Write, false, false))
            {
                shift.Write(test, 0, test.Length);
                shiftmsout.Position = 0;
                result = shiftmsout.ToArray();
            }

            Assert.True(result.Length - blockSize / 8 == test.Length);

            byte[] unshiftresult;
            using (var unshiftmsout = new MemoryStream())
            using (var unshift = new CryptoPrefixStream(unshiftmsout, blockSize, CryptoStreamMode.Write, true, false))
            {
                unshift.Write(result, 0, result.Length);
                unshiftmsout.Position = 0;
                unshiftresult = unshiftmsout.ToArray();
            }

            Assert.Equal(test.Length, unshiftresult.Length);
            for (var i = 0; i < test.Length; i++)
                Assert.Equal(test[i], unshiftresult[i]);
        }

        [Fact]
        public async Task CryptoPrefixStreamReadAsync()
        {
            var test = GetTestBytes();

            byte[] result;
            using (var shiftmsin = new MemoryStream(test))
            using (var shiftmsout = new MemoryStream())
            using (var shiftstream = new CryptoPrefixStream(shiftmsin, 256, CryptoStreamMode.Read, false, false))
            {
                await shiftstream.CopyToAsync(shiftmsout);
                result = shiftmsout.ToArray();
            }

            Assert.True(result.Length > test.Length);

            byte[] unshiftresult;
            using (var unshiftmsin = new MemoryStream(result))
            using (var unshiftmsout = new MemoryStream())
            using (var unshift = new CryptoPrefixStream(unshiftmsin, 256, CryptoStreamMode.Read, true, false))
            {
                await unshift.CopyToAsync(unshiftmsout);
                unshiftresult = unshiftmsout.ToArray();
            }

            Assert.Equal(test.Length, unshiftresult.Length);
            for (var i = 0; i < test.Length; i++)
                Assert.Equal(test[i], unshiftresult[i]);
        }

        [Fact]
        public async Task CryptoPrefixStreamWriteAsync()
        {
            var test = GetTestBytes();

            byte[] result;
            using (var shiftmsout = new MemoryStream())
            using (var shift = new CryptoPrefixStream(shiftmsout, 256, CryptoStreamMode.Write, false, false))
            {
                await shift.WriteAsync(test, 0, test.Length);
                shiftmsout.Position = 0;
                result = shiftmsout.ToArray();
            }

            Assert.True(result.Length > test.Length);

            byte[] unshiftresult;
            using (var unshiftmsout = new MemoryStream())
            using (var unshift = new CryptoPrefixStream(unshiftmsout, 256, CryptoStreamMode.Write, true, false))
            {
                await unshift.WriteAsync(result, 0, result.Length);
                unshiftmsout.Position = 0;
                unshiftresult = unshiftmsout.ToArray();
            }

            Assert.Equal(test.Length, unshiftresult.Length);
            for (var i = 0; i < test.Length; i++)
                Assert.Equal(test[i], unshiftresult[i]);
        }

        [Fact]
        public void CryptoPrefixUnique()
        {
            var test = Convert.ToBase64String(GetTestBytes());
            var key = SymmetricEncryptor.GetKey("test", null, SymmetricKeySize.Bits_256, SymmetricBlockSize.Bits_128);

            var values = new List<string>();
            for (var i = 0; i < 100; i++)
                values.Add(SymmetricEncryptor.Encrypt(SymmetricAlgorithmType.AESwithPrefix, key, test));

            Assert.True(values.Distinct().Count() == values.Count);
            var endings = values.Select(x => x.Substring(x.Length - 6, 6));
            Assert.True(endings.Distinct().Count() == values.Count);

            for (var i = 0; i < values.Count; i++)
            {
                var decrypted = SymmetricEncryptor.Decrypt(SymmetricAlgorithmType.AESwithPrefix, key, values[i]);
                Assert.Equal(test, decrypted);
            }
        }

        private static byte[] GetTestBytes()
        {
            var bytes = new byte[100000];
            for (var i = 0; i < bytes.Length; i++)
                bytes[i] = (byte)i;
            return bytes;
        }
    }
}
