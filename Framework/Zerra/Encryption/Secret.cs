// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Serialization;

namespace Zerra.Encryption
{
    public sealed class Secret<T>
    {
        private static readonly ByteSerializer byteSerializer = new ByteSerializer();
        private static readonly SymmetricKey key = SymmetricEncryptor.GenerateKey(SymmetricAlgorithmType.AES);
        private readonly byte[] secretEncrypted;
        public Secret(T secret)
        {
            var bytes = byteSerializer.Serialize(secret);
            this.secretEncrypted = SymmetricEncryptor.Encrypt(SymmetricAlgorithmType.AESwithShift, key, bytes);
        }

        private T GetSecret()
        {
            var encryptedBytes = SymmetricEncryptor.Decrypt(SymmetricAlgorithmType.AESwithShift, key, secretEncrypted);
            var secret = byteSerializer.Deserialize<T>(encryptedBytes);
            return secret;
        }

        public static bool operator ==(Secret<T> a, Secret<T> b)
        {
            if (ReferenceEquals(a, null))
            {
                if (ReferenceEquals(b, null))
                {
                    return true;
                }
                else
                {
                    if (b.GetSecret() == null)
                        return true;
                    else
                        return false;
                }
            }
            else if (ReferenceEquals(b, null))
            {
                if (ReferenceEquals(a, null))
                {
                    return true;
                }
                else
                {
                    if (a.GetSecret() == null)
                        return true;
                    else
                        return false;
                }
            }

            var asecret = a.GetSecret();
            if (asecret == null)
            {
                if (b.GetSecret() == null)
                    return true;
                else
                    return false;
            }

            var bsecret = b.GetSecret();
            if (bsecret == null)
            {
                if (bsecret == null)
                    return true;
                else
                    return false;
            }
            return asecret.Equals(bsecret);
        }
        public static bool operator !=(Secret<T> a, Secret<T> b)
        {
            if (ReferenceEquals(a, null))
            {
                if (ReferenceEquals(b, null))
                {
                    return false;
                }
                else
                {
                    if (b.GetSecret() == null)
                        return false;
                    else
                        return true;
                }
            }
            else if (ReferenceEquals(b, null))
            {
                if (a.GetSecret() == null)
                    return false;
                else
                    return true;
            }

            var asecret = a.GetSecret();
            if (asecret == null)
            {
                if (b.GetSecret() == null)
                    return false;
                else
                    return true;
            }

            var bsecret = b.GetSecret();
            if (bsecret == null)
            {
                if (bsecret == null)
                    return false;
                else
                    return true;
            }
            return asecret.Equals(bsecret);
        }

        public static bool operator ==(Secret<T> a, T b)
        {
            if (ReferenceEquals(a, null))
            {
                if (ReferenceEquals(b, null))
                    return true;
                else
                    return false;
            }
            else if (ReferenceEquals(b, null))
            {
                if (a.GetSecret() == null)
                    return true;
                else
                    return false;
            }
            var asecret = a.GetSecret();
            if (asecret == null)
                return false;
            return asecret.Equals(b);
        }
        public static bool operator !=(Secret<T> a, T b)
        {
            if (ReferenceEquals(a, null))
            {
                if (ReferenceEquals(b, null))
                    return false;
                else
                    return true;
            }
            else if (ReferenceEquals(b, null))
            {
                if (a.GetSecret() == null)
                    return false;
                else
                    return true;
            }
            var asecret = a.GetSecret();
            if (asecret == null)
                return true;
            return !asecret.Equals(b);
        }

        public static implicit operator T(Secret<T> it)
        {
            if (ReferenceEquals(it, null))
                return default;
            return it.GetSecret();
        }
        public static explicit operator Secret<T>(T it)
        {
            return new Secret<T>(it);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;

            if (ReferenceEquals(obj, null))
                return false;

            if (obj is Secret<T> secretT)
            {
                var secretTsecret = secretT.GetSecret();
                var thissecret = this.GetSecret();
                if (secretTsecret == null)
                {
                    if (thissecret == null)
                        return true;
                    else
                        return false;
                }
                if (thissecret == null)
                    return false;
                return secretTsecret.Equals(thissecret);
            }

            if (obj is T t)
            {
                var thissecret = this.GetSecret();
                if (t == null)
                {
                    if (thissecret == null)
                        return true;
                    else
                        return false;
                }
                if (thissecret == null)
                    return false;
                return t.Equals(thissecret);
            }

            return false;
        }

        public override int GetHashCode()
        {
            var secret = GetSecret();
            return secret == null ? default : secret.GetHashCode();
        }

        public override string ToString()
        {
            return "[Secret]";
        }
    }
}
