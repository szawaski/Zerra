// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using BenchmarkDotNet.Attributes;
using System.Security.Cryptography;
using Zerra.Encryption;

namespace Zerra.Benchmark.Benchmarks
{
    [MemoryDiagnoser]
    [SimpleJob(warmupCount: 2, iterationCount: 5)]
    public class EncryptorBenchmarks
    {
        [Params("AES_Old", "AESwithShift_Old", "AES_CBC", "AES_CBC_HMAC", "AES_GCM")]
        public string Algorithm { get; set; } = null!;

        [Params(256, 16 * 1024, 1024 * 1024)]
        public int Size { get; set; }

        private IEncryptor encryptor = null!;
        private byte[] plain = null!;
        private byte[] encrypted = null!;

        [GlobalSetup]
        public void Setup()
        {
            var key = RandomNumberGenerator.GetBytes(32);
            encryptor = Algorithm switch
            {
#pragma warning disable CS0612 //compares with the obsolete Old format
                "AES_Old" => new ZerraEncryptorOld(key, RandomNumberGenerator.GetBytes(16), SymmetricAlgorithmTypeOld.AES),
                "AESwithShift_Old" => new ZerraEncryptorOld(key, RandomNumberGenerator.GetBytes(16), SymmetricAlgorithmTypeOld.AESwithShift),
#pragma warning restore CS0612
                "AES_CBC" => new ZerraEncryptor(key, SymmetricAlgorithmType.AES_CBC),
                "AES_CBC_HMAC" => new ZerraEncryptor(key, SymmetricAlgorithmType.AES_CBC_HMAC),
                "AES_GCM" => new ZerraEncryptor(key, SymmetricAlgorithmType.AES_GCM),
                _ => throw new NotSupportedException(Algorithm)
            };
            plain = RandomNumberGenerator.GetBytes(Size);
            encrypted = encryptor.Encrypt(plain);
        }

        [Benchmark]
        public byte[] Encrypt()
        {
            return encryptor.Encrypt(plain);
        }

        [Benchmark]
        public byte[] Decrypt()
        {
            return encryptor.Decrypt(encrypted);
        }
    }
}
