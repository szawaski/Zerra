[← Back to Documentation](Index.md)

# Encryptors

Zerra provides built-in encryption capabilities to secure messages transmitted between services. The `ZerraEncryptor` class implements symmetric encryption using various algorithms to protect sensitive data in transit.

## Overview

Encryption in Zerra:
- Transparent to application code - encryption/decryption happens automatically
- Symmetric encryption using shared keys
- Support for multiple algorithms (AES, DES, TripleDES, RC2)
- Optional prefix mode for algorithm identification
- Configured once and applied to all messages

## ZerraEncryptor

The `ZerraEncryptor` class provides symmetric encryption for message payloads using .NET's cryptographic providers.

### Features

- **Transparent Encryption**: Automatically encrypts/decrypts all messages
- **Multiple Algorithms**: AES, DES, TripleDES, RC2 support
- **Prefix Mode**: Optional algorithm identifier in encrypted data
- **Secure**: Uses industry-standard cryptographic algorithms
- **Simple Configuration**: Single setup for all message types

## Usage

### Basic Setup

```csharp
using Zerra.Encryption;

// Create encryptor with password and algorithm
IEncryptor encryptor = new ZerraEncryptor(
    password: "mySecurePassword123!",
    algorithmType: SymmetricAlgorithmType.AESwithPrefix
);

// Use in Bus configuration
var bus = Bus.New(
    service: "MyService",
    log: logger,
    busLog: busLogger,
    busScopes: busScopes
);

// Use in network components
var server = new TcpCqrsServer("localhost:9001", serializer, encryptor, log);
var client = new TcpCqrsClient("localhost:9001", serializer, encryptor, log);

var httpServer = new HttpCqrsServer("localhost:8080", serializer, encryptor, log);
var httpClient = new HttpCqrsClient("localhost:8080", serializer, encryptor, log);
```

### Without Encryption

If encryption is not needed, pass `null` for the encryptor parameter:

```csharp
// No encryption
var server = new TcpCqrsServer("localhost:9001", serializer, null, log);
var client = new TcpCqrsClient("localhost:9001", serializer, null, log);
```

## Supported Algorithms

Zerra supports several symmetric encryption algorithms through the `SymmetricAlgorithmType` enum:

### AES (Advanced Encryption Standard) - Recommended

```csharp
// AES without prefix
IEncryptor encryptor = new ZerraEncryptor("myPassword", SymmetricAlgorithmType.AES);

// AES with prefix (recommended for version compatibility)
IEncryptor encryptor = new ZerraEncryptor("myPassword", SymmetricAlgorithmType.AESwithPrefix);
```

**Characteristics:**
- ✅ Industry standard
- ✅ Excellent security
- ✅ Good performance
- ✅ Recommended for production use

### DES (Data Encryption Standard)

```csharp
// DES without prefix
IEncryptor encryptor = new ZerraEncryptor("myPassword", SymmetricAlgorithmType.DES);

// DES with prefix
IEncryptor encryptor = new ZerraEncryptor("myPassword", SymmetricAlgorithmType.DESwithPrefix);
```

**Characteristics:**
- ⚠️ Weaker than AES
- ⚠️ Use only for legacy compatibility
- ✅ Faster than TripleDES

### TripleDES (3DES)

```csharp
// TripleDES without prefix
IEncryptor encryptor = new ZerraEncryptor("myPassword", SymmetricAlgorithmType.TripleDES);

// TripleDES with prefix
IEncryptor encryptor = new ZerraEncryptor("myPassword", SymmetricAlgorithmType.TripleDESwithPrefix);
```

**Characteristics:**
- ⚠️ Slower than AES
- ⚠️ Being phased out
- ✅ Stronger than DES

### RC2

```csharp
// RC2 without prefix
IEncryptor encryptor = new ZerraEncryptor("myPassword", SymmetricAlgorithmType.RC2);

// RC2 with prefix
IEncryptor encryptor = new ZerraEncryptor("myPassword", SymmetricAlgorithmType.RC2withPrefix);
```

**Characteristics:**
- ⚠️ Less common
- ⚠️ Use only for specific compatibility needs

## Prefix Mode

The "withPrefix" variants add a random prefix to encrypted data, which combined with CBC (Cipher Block Chaining) mode ensures that encrypting the same plaintext multiple times produces different ciphertext each time. This provides several important benefits:

### How It Works

```csharp
// With prefix mode
IEncryptor encryptor = new ZerraEncryptor("myPassword", SymmetricAlgorithmType.AESwithPrefix);

// Encrypting the same data twice produces different results
var data = "Hello World";
var encrypted1 = encryptor.Encrypt(data);  // e.g., [random prefix 1][encrypted bytes 1]
var encrypted2 = encryptor.Encrypt(data);  // e.g., [random prefix 2][encrypted bytes 2]

// encrypted1 != encrypted2 (different ciphertext for same plaintext)
// But both decrypt back to "Hello World"
```

### Security Benefit

Prefix Mode provides critical security enhancement: the random prefix combined with CBC (Cipher Block Chaining) mode ensures that encrypting the same plaintext multiple times produces different ciphertext each time. This prevents pattern analysis and makes it impossible for attackers to identify when the same data is being transmitted repeatedly.

### With vs Without Prefix

```csharp
// ✅ With prefix - recommended for production
IEncryptor withPrefix = new ZerraEncryptor("myPassword", SymmetricAlgorithmType.AESwithPrefix);
// Same plaintext → Different ciphertext each time
// More secure against pattern analysis
// Slightly larger payload (prefix overhead)

// Without prefix - deterministic encryption
IEncryptor withoutPrefix = new ZerraEncryptor("myPassword", SymmetricAlgorithmType.AES);
// Same plaintext → Same ciphertext every time
// Vulnerable to pattern analysis
// Slightly smaller payload
```

### Security Example

```csharp
// Without prefix mode (less secure)
var encryptor = new ZerraEncryptor("myPassword", SymmetricAlgorithmType.AES);
var command1 = new CreateUserCommand { Email = "user@example.com" };
var command2 = new CreateUserCommand { Email = "user@example.com" };
// If serialized identically, encrypted payloads will be identical
// Attacker can see repeated patterns

// With prefix mode (more secure)
var encryptor = new ZerraEncryptor("myPassword", SymmetricAlgorithmType.AESwithPrefix);
var command1 = new CreateUserCommand { Email = "user@example.com" };
var command2 = new CreateUserCommand { Email = "user@example.com" };
// Encrypted payloads will be different due to random prefix + CBC
// Attacker cannot detect repeated patterns
```

## Algorithm Comparison

| Algorithm | Security | Performance | Key Size | Recommendation |
|-----------|----------|-------------|----------|----------------|
| AES | ⭐⭐⭐⭐⭐ | ⚡⚡⚡⚡⚡ | 128-256 bit | ✅ Use for production |
| TripleDES | ⭐⭐⭐ | ⚡⚡ | 168 bit | ⚠️ Legacy only |
| DES | ⭐ | ⚡⚡⚡⚡ | 56 bit | ❌ Avoid |
| RC2 | ⭐⭐ | ⚡⚡⚡ | 40-128 bit | ⚠️ Special cases |

## Password Requirements

The encryption password/key is critical for security. Follow these guidelines:

### ✅ Good Passwords

```csharp
// Strong password with complexity
IEncryptor encryptor = new ZerraEncryptor("P@ssw0rd!Secur3#2024", SymmetricAlgorithmType.AESwithPrefix);

// Use environment variables in production
var password = Environment.GetEnvironmentVariable("ENCRYPTION_KEY");
IEncryptor encryptor = new ZerraEncryptor(password, SymmetricAlgorithmType.AESwithPrefix);

// Use configuration management
var password = configuration["Encryption:Key"];
IEncryptor encryptor = new ZerraEncryptor(password, SymmetricAlgorithmType.AESwithPrefix);
```

### ❌ Bad Passwords

```csharp
// Too simple
IEncryptor encryptor = new ZerraEncryptor("test", SymmetricAlgorithmType.AESwithPrefix);

// Hardcoded in production code
IEncryptor encryptor = new ZerraEncryptor("password123", SymmetricAlgorithmType.AESwithPrefix);

// Too short
IEncryptor encryptor = new ZerraEncryptor("abc", SymmetricAlgorithmType.AESwithPrefix);
```

## Encryptor Compatibility

⚠️ **Critical**: Both client and server must use:
1. The same encryption password
2. The same algorithm
3. The same prefix mode

```csharp
// ❌ Will NOT work - different passwords
// Server
var serverEncryptor = new ZerraEncryptor("serverPassword", SymmetricAlgorithmType.AESwithPrefix);

// Client
var clientEncryptor = new ZerraEncryptor("clientPassword", SymmetricAlgorithmType.AESwithPrefix);

// ❌ Will NOT work - different algorithms
// Server
var serverEncryptor = new ZerraEncryptor("password", SymmetricAlgorithmType.AES);

// Client
var clientEncryptor = new ZerraEncryptor("password", SymmetricAlgorithmType.DES);

// ✅ Correct - matching configuration
// Server
var serverEncryptor = new ZerraEncryptor("sharedPassword", SymmetricAlgorithmType.AESwithPrefix);

// Client  
var clientEncryptor = new ZerraEncryptor("sharedPassword", SymmetricAlgorithmType.AESwithPrefix);
```

## Best Practices

### 1. Use AES with Prefix

```csharp
IEncryptor encryptor = new ZerraEncryptor(
    password: GetSecurePassword(),
    algorithmType: SymmetricAlgorithmType.AESwithPrefix
);
```

### 2. Store Keys Securely

```csharp
// Use Azure Key Vault, AWS Secrets Manager, or similar
var keyVaultClient = new SecretClient(vaultUri, credential);
var secret = await keyVaultClient.GetSecretAsync("EncryptionKey");
IEncryptor encryptor = new ZerraEncryptor(secret.Value.Value, SymmetricAlgorithmType.AESwithPrefix);
```

### 3. Rotate Keys Periodically

```csharp
// Support multiple keys during rotation
var currentKey = GetCurrentEncryptionKey();
var encryptor = new ZerraEncryptor(currentKey, SymmetricAlgorithmType.AESwithPrefix);
```

### 4. Use Environment-Specific Keys

```csharp
// Different keys for dev, staging, production
var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
var key = configuration[$"Encryption:{environment}:Key"];
IEncryptor encryptor = new ZerraEncryptor(key, SymmetricAlgorithmType.AESwithPrefix);
```

### 5. Never Commit Keys to Source Control

```csharp
// ❌ Don't do this
IEncryptor encryptor = new ZerraEncryptor("hardcodedKey", SymmetricAlgorithmType.AESwithPrefix);

// ✅ Do this
IEncryptor encryptor = new ZerraEncryptor(
    configuration["Encryption:Key"],
    SymmetricAlgorithmType.AESwithPrefix
);
```

## Custom Encryption

To implement custom encryption, create a class implementing `IEncryptor`:

```csharp
public interface IEncryptor
{
    byte[] Encrypt(byte[] data);
    byte[] Decrypt(byte[] data);
}

public class CustomEncryptor : IEncryptor
{
    public byte[] Encrypt(byte[] data)
    {
        // Your custom encryption logic
    }

    public byte[] Decrypt(byte[] data)
    {
        // Your custom decryption logic
    }
}
```

## Common Scenarios

### Production Environment

```csharp
// Load from secure configuration
var encryptionKey = configuration["Encryption:Key"];
if (string.IsNullOrEmpty(encryptionKey))
    throw new InvalidOperationException("Encryption key not configured");

IEncryptor encryptor = new ZerraEncryptor(
    encryptionKey,
    SymmetricAlgorithmType.AESwithPrefix
);
```

### Development Environment

```csharp
// Use simple key for development (never in production!)
#if DEBUG
    IEncryptor encryptor = new ZerraEncryptor("devKey123", SymmetricAlgorithmType.AESwithPrefix);
#else
    IEncryptor encryptor = new ZerraEncryptor(configuration["Encryption:Key"], SymmetricAlgorithmType.AESwithPrefix);
#endif
```

### No Encryption (Development Only)

```csharp
// Disable encryption for local debugging
IEncryptor encryptor = null;
var server = new TcpCqrsServer("localhost:9001", serializer, encryptor, log);
```

## Security Considerations

1. **Key Management**: Use secure key storage (Azure Key Vault, AWS KMS, etc.)
2. **Transport Security**: Consider TLS/SSL in addition to message encryption
3. **Algorithm Selection**: Always use AES unless you have specific requirements
4. **Password Complexity**: Use strong, random passwords of appropriate length
5. **Avoid Hardcoding**: Never hardcode encryption keys in source code

## See Also

- [Serializers](Serializers.md) - Message serialization configuration
- [Server Setup](ServerSetup.md) - Server-side encryption setup
- [Client Setup](ClientSetup.md) - Client-side encryption setup
