using System.Security.Cryptography;
using System.Text;
using Zerra.Encryption;

namespace Store.Common
{
    /// <summary>
    /// Rotates the encryption key between services on a schedule without a key server: every service derives the key for the current period
    /// from the shared secret, so they all move to the same new key together. A message still in flight from the last period decrypts with the previous key.
    /// A real deployment would read the current key from a secret store instead.
    /// </summary>
    public sealed class StoreKeySource : ISymmetricKeySource
    {
        private readonly byte[] secret;
        private readonly TimeSpan period;

        public StoreKeySource(string sharedSecret, TimeSpan period)
        {
            this.secret = SymmetricEncryptor.DeriveKey(sharedSecret);
            this.period = period;
        }

        public (byte[] Key, DateTime Expiration) GetKey()
        {
            var number = DateTime.UtcNow.Ticks / period.Ticks;
            var key = HMACSHA256.HashData(secret, Encoding.ASCII.GetBytes($"Store key {number}"));
            return (key, new DateTime((number + 1) * period.Ticks, DateTimeKind.Utc));
        }

        public ValueTask<(byte[] Key, DateTime Expiration)> GetKeyAsync() => new(GetKey());
    }
}
