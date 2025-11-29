
namespace Zerra.Encryption
{
    public sealed class ZerraEncryptor : IEncryptor
    {
        private readonly SymmetricConfig config;
        public ZerraEncryptor(string key, SymmetricAlgorithmType algorithm)
        {
            var symmetricKey = SymmetricEncryptor.GetKey(key);
            config = new SymmetricConfig(algorithm, symmetricKey);
        }

        public byte[] Encrypt(byte[] bytes)
            => SymmetricEncryptor.Encrypt(config, bytes);

        public byte[] Decrypt(byte[] bytes)
            => SymmetricEncryptor.Decrypt(config, bytes);

#if !NETSTANDARD2_0
        public Span<byte> Encrypt(ReadOnlySpan<byte> bytes)
            => SymmetricEncryptor.Encrypt(config, bytes);

        public Span<byte> Decrypt(ReadOnlySpan<byte> bytes)
            => SymmetricEncryptor.Decrypt(config, bytes);
#endif

        public CryptoFlushStream Encrypt(Stream stream, bool write)
            => SymmetricEncryptor.Encrypt(config, stream, write, false);

        public CryptoFlushStream Decrypt(Stream stream, bool write)
            => SymmetricEncryptor.Decrypt(config, stream, write, false);
    }
}
