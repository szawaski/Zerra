[← Back to Documentation](Index.md)

# Encryptors

An `IEncryptor` encrypts every message a server, client, producer, or consumer sends, and decrypts what it receives. `ZerraEncryptor` is the built-in implementation, using a shared key with a symmetric algorithm. Pass `null` instead of an encryptor to send messages unencrypted.

```csharp
using Zerra.Encryption;

IEncryptor encryptor = new ZerraEncryptor(configuration["Encryption:Key"], SymmetricAlgorithmType.AES_GCM);

var server = new TcpCqrsServer("localhost:9001", serializer, encryptor, null, log);
var client = new TcpCqrsClient("localhost:9001", serializer, encryptor, null, log);
```

Like serializers, encryptors are passed to each server, client, producer, and consumer, not to `Bus.New`. The argument after the encryptor is an optional [compressor](Compressors.md), which runs before encryption when sending and after decryption when receiving.

## Configuration

```csharp
public ZerraEncryptor(
    string key,                                           // password the key is derived from
    SymmetricAlgorithmType algorithm,
    SymmetricKeySize keySize = SymmetricKeySize.Bits_256,
    HashAlgorithmName? hashAlgorithm = null,              // hash used to derive the key, SHA256
    int deriveKeyIterations = 1000)

public ZerraEncryptor(byte[] key, SymmetricAlgorithmType algorithm)   // the key itself, 16, 24, or 32 bytes
```

**Every value must match on both ends:** the key, the algorithm, and any key size, hash, or iteration count you override. Anything else fails to decrypt.

Use a long random key, not a word or phrase: the key is stretched with PBKDF2, but a guessable one can still be found by trying candidates. 32 random bytes as Base64 is plenty:

```csharp
var key = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
```

Keep the key out of source control. Read it from configuration or a secret store such as Azure Key Vault, and use a different key per environment.

## Algorithms

| `SymmetricAlgorithmType` | Keeps data private | Rejects changed, reordered, or cut-off data | Platforms |
|---|---|---|---|
| `AES_GCM` | yes | yes | .NET Core 3.0 and later. **Recommended** |
| `AES_CBC_HMAC` | yes | yes | all, including .NET Framework |
| `AES_CBC` | yes | no | all |

- **`AES_GCM`** is the fastest of the tamper-proof modes. On .NET Standard it throws `PlatformNotSupportedException`, so use `AES_CBC_HMAC` when a .NET Framework service shares the key.
- **`AES_CBC_HMAC`** encrypts with AES-CBC and then signs each chunk with HMAC-SHA256.
- **`AES_CBC`** only keeps data private. Someone without the key can't read it, but can change bytes in it without being detected.

Data is encrypted in chunks of up to 64 KB. Each chunk gets a random IV that travels unencrypted in front of it (an IV only has to be unpredictable, not secret), and `AES_GCM` gives each message its own key from a random value sent the same way, so the same message never encrypts to the same bytes and large streams don't have to be buffered. A tamper-proof mode checks each chunk before handing it on, and fails with a `CryptographicException` when anything was changed or the key is wrong.

The modes aren't interchangeable, so both ends must use the same one.

### Format

For reading or writing Zerra's encrypted data in another language. A message is a header, then chunks. Each chunk is a 4-byte little-endian length of its body, with the high bit set on the last chunk, then the body. A chunk holds up to 64 KB of data, chunks are numbered from 0, and a message always ends with a last chunk, which can be empty. Encrypting zero bytes as a byte array returns zero bytes.

| Mode | Header | Chunk body |
|---|---|---|
| `AES_GCM` | 16 random bytes | ciphertext, then the 16-byte tag |
| `AES_CBC_HMAC` | 16 random bytes | 16-byte random IV, ciphertext with PKCS7 padding, then a 32-byte HMAC |
| `AES_CBC` | none | 16-byte random IV, then ciphertext with PKCS7 padding |

- **`AES_GCM`:** the message's key is HMAC-SHA256 of the header with the key, cut to the key's length. The 12-byte nonce is 8 zero bytes then the chunk number as a big-endian `uint32`, and the 4 length bytes are the associated data.
- **`AES_CBC_HMAC`:** the MAC key is HMAC-SHA256 of the ASCII text `Zerra AES_CBC_HMAC` with the key. Each chunk's HMAC-SHA256 covers the header, the chunk number as a big-endian `uint32`, the 4 length bytes, the IV, and the ciphertext.
- **Keys from a password:** PBKDF2 of the UTF-8 password, 1,000 iterations, SHA-256, and as its salt, SHA-256 of the password followed by the salt, then the salt. The salt is the UTF-8 of the one given, or of `ενγρυπτιον`.

`SymmetricEncryptor` has the same modes for data that isn't a message, such as values you store, with keys from `SymmetricEncryptor.DeriveKey(password)` or `GenerateKey()`.

### Data Encrypted With the Old Format

The old `AESwithShift` and plain `AES` formats don't detect changed data. Data already stored in them can still be read and written with `ZerraEncryptorOld`, which derives the key from the password the old way, or `SymmetricEncryptorOld` with `SymmetricAlgorithmTypeOld`. They're obsolete, so code that uses them gets warning `CS0612` as a reminder to move that data to a new mode. Use them only for that data ([Upgrading](UpgradeV5ToV6.md)):

```csharp
IEncryptor encryptor = new ZerraEncryptorOld(password);   // AESwithShift
```

### What Encryption Doesn't Do

- **It doesn't prove who sent a message.** Every service holding the key can send any message, so a compromised service can send anything. Internal services are trusted because of where they run ([Security](Security.md#trust-model)).
- **It doesn't stop replays.** A captured message can be sent again, even in a tamper-proof mode. Make commands that matter idempotent ([Commands](Commands.md#idempotency)).
- **It doesn't replace TLS.** `TcpCqrsServer` and `HttpCqrsServer` have no TLS, so on a network you don't trust, host the service in ASP.NET Core with HTTPS ([Zerra.Web](ZerraWeb.md)) or use a service mesh with mutual TLS.

## Custom Encryptors

Implement `IEncryptor`:

```csharp
public interface IEncryptor
{
    byte[] Encrypt(byte[] bytes);
    byte[] Decrypt(byte[] bytes);
    Span<byte> Encrypt(ReadOnlySpan<byte> bytes);             // not on .NET Standard 2.0
    Span<byte> Decrypt(ReadOnlySpan<byte> bytes);
    CryptoFlushStream Encrypt(Stream stream, bool write);     // for streamed payloads
    CryptoFlushStream Decrypt(Stream stream, bool write);
}
```

`CryptoFlushStream` (in `Zerra.Encryption`) wraps a stream and adds `FlushFinalBlock` and `FlushFinalBlockAsync`.

## Asymmetric Encryption

`AsymmetricEncryptor` encrypts data that only the holder of a private key can decrypt, for when the sender shouldn't be able to read it back or doesn't share a secret with the receiver. The result is standard JSON Web Encryption (RFC 7516) in compact form: the data is encrypted under a random key, and that key with RSA. So the data can be any size, changed data is rejected, and any JOSE library in any language can decrypt it.

```csharp
var keys = AsymmetricEncryptor.GenerateKey();                         // 2048-bit RSA, keys as PEM
var encrypted = AsymmetricEncryptor.Encrypt(AsymmetricAlgorithmType.RSA_OAEP_256_A256GCM, keys.PublicKey, "secret");
var plain = AsymmetricEncryptor.Decrypt(keys.PrivateKey!, encrypted);  // the JWE header says which algorithms
```

| `AsymmetricAlgorithmType` | JWE `alg` and `enc` | Platforms |
|---|---|---|
| `RSA_OAEP_256_A256GCM` | `RSA-OAEP-256`, `A256GCM` | .NET Core 3.0 and later. **Recommended** |
| `RSA_OAEP_A256CBC_HS512` | `RSA-OAEP`, `A256CBC-HS512` | all, including .NET Framework |

Keys are PEM (`PUBLIC KEY`, `PRIVATE KEY`, or the `RSA` forms), so keys from other tools (OpenSSL, a key vault) work too. Keys under 2048 bits are rejected.

## Hashing Passwords

`Hasher.PBKDF2GenerateHash` hashes a password for storage as a PHC string, such as `$pbkdf2-sha256$i=600000$salt$hash`. The string holds the algorithm and iterations, so `PBKDF2VerifyHash` checks it however it was made, and `PBKDF2NeedsRehash` says when to hash a password again with stronger settings, such as the next time the user signs in.

```csharp
var stored = Hasher.PBKDF2GenerateHash(password);              // SHA-256, 600,000 iterations
if (Hasher.PBKDF2VerifyHash(attempt, stored) && Hasher.PBKDF2NeedsRehash(stored))
    stored = Hasher.PBKDF2GenerateHash(attempt);
```

`Hasher.GenerateHash` is a single fast SHA-2 hash for checking data, not for passwords. `HasherOld` checks hashes stored in the old format, including MD5 and SHA-1 ones. `Password.GeneratePassword` makes random passwords with at least one character from each set chosen.

## See Also

- [Serializers](Serializers.md) - What gets encrypted
- [Compressors](Compressors.md) - Compressing before encrypting
- [Security](Security.md) - Authorization and claims
