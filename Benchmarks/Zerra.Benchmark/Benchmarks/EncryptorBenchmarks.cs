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

        //the transports write and read through the streams, the serializers use 8 KB pieces
        [Benchmark]
        public async Task<long> EncryptStream()
        {
            var sink = new SinkStream();
            await using (var stream = encryptor.Encrypt(sink, true))
            {
                for (var offset = 0; offset < plain.Length; offset += pieceSize)
                    await stream.WriteAsync(plain.AsMemory(offset, Math.Min(pieceSize, plain.Length - offset)));
                await stream.FlushFinalBlockAsync();
            }
            return sink.Count;
        }

        [Benchmark]
        public async Task<long> DecryptStream()
        {
            var total = 0L;
            await using (var stream = encryptor.Decrypt(new MemoryStream(encrypted, false), false))
            {
                int read;
                while ((read = await stream.ReadAsync(readBuffer)) > 0)
                    total += read;
            }
            return total;
        }

        private const int pieceSize = 8 * 1024;
        private readonly byte[] readBuffer = new byte[pieceSize];

        private sealed class SinkStream : Stream
        {
            public long Count;
            public override bool CanRead => false;
            public override bool CanSeek => false;
            public override bool CanWrite => true;
            public override long Length => Count;
            public override long Position { get => Count; set => throw new NotSupportedException(); }
            public override void Flush() { }
            //the base one queues the flush to the thread pool, a network stream doesn't
            public override Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;
            public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => Count += count;
            public override void Write(ReadOnlySpan<byte> buffer) => Count += buffer.Length;
            public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
            {
                Count += buffer.Length;
                return default;
            }
        }
    }
}
