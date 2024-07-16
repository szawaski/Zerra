// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Zerra.Encryption;
using Zerra.Serialization.Bytes;

namespace Zerra.Test
{
    [TestClass]
    public class EncryptionTest
    {
        [TestMethod]
        public void CryptoShiftStreamRead()
        {
            const int blockSize = 256;
            var test = GetTestBytes();

            byte[] result;
            using (var shiftmsin = new MemoryStream(test))
            using (var shiftmsout = new MemoryStream())
            using (var shiftstream = new CryptoShiftStream(shiftmsin, blockSize, CryptoStreamMode.Read, false, false))
            {
                shiftstream.CopyTo(shiftmsout);
                result = shiftmsout.ToArray();
            }

            Assert.IsTrue(result.Length - blockSize / 8 == test.Length);

            byte[] unshiftresult;
            using (var unshiftmsin = new MemoryStream(result))
            using (var unshiftmsout = new MemoryStream())
            using (var unshift = new CryptoShiftStream(unshiftmsin, blockSize, CryptoStreamMode.Read, true, false))
            {
                unshift.CopyTo(unshiftmsout);
                unshiftresult = unshiftmsout.ToArray();
            }

            Assert.AreEqual(test.Length, unshiftresult.Length);
            for (var i = 0; i < test.Length; i++)
                Assert.AreEqual(test[i], unshiftresult[i]);
        }

        [TestMethod]
        public void CryptoShiftStreamWrite()
        {
            const int blockSize = 256;
            var test = GetTestBytes();

            byte[] result;
            using (var shiftmsout = new MemoryStream())
            using (var shift = new CryptoShiftStream(shiftmsout, blockSize, CryptoStreamMode.Write, false, false))
            {
                shift.Write(test, 0, test.Length);
                shiftmsout.Position = 0;
                result = shiftmsout.ToArray();
            }

            Assert.IsTrue(result.Length - blockSize / 8 == test.Length);

            byte[] unshiftresult;
            using (var unshiftmsout = new MemoryStream())
            using (var unshift = new CryptoShiftStream(unshiftmsout, blockSize, CryptoStreamMode.Write, true, false))
            {
                unshift.Write(result, 0, result.Length);
                unshiftmsout.Position = 0;
                unshiftresult = unshiftmsout.ToArray();
            }

            Assert.AreEqual(test.Length, unshiftresult.Length);
            for (var i = 0; i < test.Length; i++)
                Assert.AreEqual(test[i], unshiftresult[i]);
        }

        [TestMethod]
        public async Task CryptoShiftStreamReadAsync()
        {
            var test = GetTestBytes();

            byte[] result;
            using (var shiftmsin = new MemoryStream(test))
            using (var shiftmsout = new MemoryStream())
            using (var shiftstream = new CryptoShiftStream(shiftmsin, 256, CryptoStreamMode.Read, false, false))
            {
                await shiftstream.CopyToAsync(shiftmsout);
                result = shiftmsout.ToArray();
            }

            Assert.IsTrue(result.Length > test.Length);

            byte[] unshiftresult;
            using (var unshiftmsin = new MemoryStream(result))
            using (var unshiftmsout = new MemoryStream())
            using (var unshift = new CryptoShiftStream(unshiftmsin, 256, CryptoStreamMode.Read, true, false))
            {
                await unshift.CopyToAsync(unshiftmsout);
                unshiftresult = unshiftmsout.ToArray();
            }

            Assert.AreEqual(test.Length, unshiftresult.Length);
            for (var i = 0; i < test.Length; i++)
                Assert.AreEqual(test[i], unshiftresult[i]);
        }

        [TestMethod]
        public async Task CryptoShiftStreamWriteAsync()
        {
            var test = GetTestBytes();

            byte[] result;
            using (var shiftmsout = new MemoryStream())
            using (var shift = new CryptoShiftStream(shiftmsout, 256, CryptoStreamMode.Write, false, false))
            {
                await shift.WriteAsync(test, 0, test.Length);
                shiftmsout.Position = 0;
                result = shiftmsout.ToArray();
            }

            Assert.IsTrue(result.Length > test.Length);

            byte[] unshiftresult;
            using (var unshiftmsout = new MemoryStream())
            using (var unshift = new CryptoShiftStream(unshiftmsout, 256, CryptoStreamMode.Write, true, false))
            {
                await unshift.WriteAsync(result, 0, result.Length);
                unshiftmsout.Position = 0;
                unshiftresult = unshiftmsout.ToArray();
            }

            Assert.AreEqual(test.Length, unshiftresult.Length);
            for (var i = 0; i < test.Length; i++)
                Assert.AreEqual(test[i], unshiftresult[i]);
        }

        [TestMethod]
        public void CryptoShiftUnique()
        {
            var test = Convert.ToBase64String(GetTestBytes());
            var key = SymmetricEncryptor.GetKey("test", null, SymmetricKeySize.Bits_256, SymmetricBlockSize.Bits_128);

            var values = new List<string>();
            for (var i = 0; i < 100; i++)
                values.Add(SymmetricEncryptor.Encrypt(SymmetricAlgorithmType.AESwithShift, key, test));

            Assert.IsTrue(values.Distinct().Count() == values.Count);

            for (var i = 0; i < values.Count; i++)
            {
                var decrypted = SymmetricEncryptor.Decrypt(SymmetricAlgorithmType.AESwithShift, key, values[i]);
                Assert.AreEqual(test, decrypted);
            }
        }

        [TestMethod]
        public void SymmetricEncryptorWithShift()
        {
            var test = Convert.ToBase64String(GetTestBytes());
            var key = SymmetricEncryptor.GetKey("test", null, SymmetricKeySize.Bits_256, SymmetricBlockSize.Bits_128);

            var encrypted = SymmetricEncryptor.Encrypt(SymmetricAlgorithmType.AESwithShift, key, test);

            string result;
            using (var ms = new MemoryStream(Convert.FromBase64String(encrypted)))
            using (var decryptionStream = SymmetricEncryptor.Decrypt(SymmetricAlgorithmType.AESwithShift, key, ms, false))
            {
                var msout = new MemoryStream();
                decryptionStream.CopyTo(msout);
                result = Encoding.UTF8.GetString(msout.ToArray());
            }

            Assert.AreEqual(test, result);
        }

        [TestMethod]
        public async Task WithSerializer()
        {
            var key = SymmetricEncryptor.GenerateKey(SymmetricAlgorithmType.AESwithShift);
            var options = new ByteSerializerOptions() { IndexSize = ByteSerializerIndexSize.UInt16 };

            var model1 = TypesAllModel.Create();
            using (var ms = new MemoryStream())
            using (var cryptoStreamWriter = SymmetricEncryptor.Encrypt(SymmetricAlgorithmType.AES, key, ms, true))
            using (var cryptoStreamReader = SymmetricEncryptor.Decrypt(SymmetricAlgorithmType.AES, key, ms, false))
            {
                var expected = ByteSerializer.Serialize(model1, options);
                await ByteSerializer.SerializeAsync(cryptoStreamWriter, model1, options);
                cryptoStreamWriter.FlushFinalBlock();
                ms.Position = 0;
                var bytes = ms.ToArray();
                var model2 = await ByteSerializer.DeserializeAsync<TypesAllModel>(cryptoStreamReader, options);
                AssertHelper.AreEqual(model1, model2);
            }
        }

        [TestMethod]
        public async Task WithSerializerAndShift()
        {
            var key = SymmetricEncryptor.GenerateKey(SymmetricAlgorithmType.AESwithShift);
            var options = new ByteSerializerOptions() { IndexSize = ByteSerializerIndexSize.UInt16 };

            var model1 = TypesAllModel.Create();
            using (var ms = new MemoryStream())
            using (var cryptoStreamWriter = SymmetricEncryptor.Encrypt(SymmetricAlgorithmType.AESwithShift, key, ms, true))
            using (var cryptoStreamReader = SymmetricEncryptor.Decrypt(SymmetricAlgorithmType.AESwithShift, key, ms, false))
            {
                var expected = ByteSerializer.Serialize(model1, options);
                await ByteSerializer.SerializeAsync(cryptoStreamWriter, model1, options);
                cryptoStreamWriter.FlushFinalBlock();
                ms.Position = 0;
                var bytes = ms.ToArray();
                var model2 = await ByteSerializer.DeserializeAsync<TypesAllModel>(cryptoStreamReader, options);
                AssertHelper.AreEqual(model1, model2);
            }
        }

        [TestMethod]
        public void EmptyStream()
        {
            var result = Array.Empty<byte>();

            var key = SymmetricEncryptor.GenerateKey(SymmetricAlgorithmType.AESwithShift);

            using (var ms = new MemoryStream(result))
            using (var cryptoStreamReader = SymmetricEncryptor.Decrypt(SymmetricAlgorithmType.AES, key, ms, false))
            using (var sr = new StreamReader(cryptoStreamReader))
            {
                sr.ReadToEnd();
            }
        }

        private static byte[] GetTestBytes()
        {
            var bytes = new List<byte>();
            for (var i = 0; i < 100000; i++)
                bytes.Add((byte)i);
            var test = bytes.ToArray();
            return test;
        }

        [TestMethod]
        public void GeneratePassword()
        {
            var test = Password.GeneratePassword(10, true, true, true, true);
            Assert.AreEqual(10, test.Length);

            test = Password.GeneratePassword(10, true, false, false, false);
            Assert.AreEqual(10, test.Length);
            Assert.IsTrue(test.All(Char.IsUpper));

            test = Password.GeneratePassword(10, false, true, false, false);
            Assert.AreEqual(10, test.Length);
            Assert.IsTrue(test.All(Char.IsLower));

            test = Password.GeneratePassword(10, false, false, true, false);
            Assert.AreEqual(10, test.Length);
            Assert.IsTrue(test.All(Char.IsNumber));

            test = Password.GeneratePassword(10, false, false, false, true);
            Assert.AreEqual(10, test.Length);
            Assert.IsTrue(test.All(x => !Char.IsLetterOrDigit(x)));
        }
    }
}
