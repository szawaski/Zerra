[← Back to Documentation](Index.md)

# Encryptors

An `IEncryptor` encrypts every message a server, client, producer, or consumer sends, and decrypts what it receives. `ZerraEncryptor` is the built-in implementation, using a shared key with a symmetric algorithm. Pass `null` instead of an encryptor to send messages unencrypted.

```csharp
using Zerra.Encryption;

IEncryptor encryptor = new ZerraEncryptor(configuration["Encryption:Key"], SymmetricAlgorithmType.AESwithPrefix);

var server = new TcpCqrsServer("localhost:9001", serializer, encryptor, log);
var client = new TcpCqrsClient("localhost:9001", serializer, encryptor, log);
```

Like serializers, encryptors are passed to each server, client, producer, and consumer, not to `Bus.New`.

## Configuration

```csharp
public ZerraEncryptor(
    string key,                                           // password the symmetric key is derived from
    SymmetricAlgorithmType algorithm,
    SymmetricKeySize keySize = SymmetricKeySize.Bits_256,
    SymmetricBlockSize blockSize = SymmetricBlockSize.Bits_128,
    HashAlgorithmName? hashAlgorithm = null,              // hash used to derive the key
    int deriveKeyIterations = 1000)
```

**Every value must match on both ends:** the key, the algorithm and its mode, and any key size, block size, hash, or iteration count you override. Anything else fails to decrypt.

Keep the key out of source control. Read it from configuration or a secret store such as Azure Key Vault, and use a different key per environment.

On .NET Standard 2.0, key derivation supports only `HashAlgorithmName.SHA1`; any other algorithm throws `PlatformNotSupportedException`.

## Algorithms

`SymmetricAlgorithmType` combines an algorithm with a mode:

| Algorithm | Plain | Prefix | Shift | Use |
|---|---|---|---|---|
| AES | `AES` | `AESwithPrefix` | `AESwithShift` | **recommended** |
| TripleDES | `TripleDES` | `TripleDESwithPrefix` | `TripleDESwithShift` | legacy only |
| DES | `DES` | `DESwithPrefix` | `DESwithShift` | avoid, weak |
| RC2 | `RC2` | `RC2withPrefix` | `RC2withShift` | legacy only |

- **Plain** is deterministic: the same message always encrypts to the same bytes, so an observer can tell when a message repeats.
- **Prefix** adds a random prefix that, with CBC chaining, makes every encryption of the same message different. Use `AESwithPrefix` unless you have a reason not to.
- **Shift** reaches the same result by inserting a random block that shifts the others.

The modes aren't interchangeable, so both ends must use the same one.

Message encryption doesn't replace TLS on connections that cross untrusted networks.

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

## See Also

- [Serializers](Serializers.md) - What gets encrypted
- [Security](Security.md) - Authorization and claims
