﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.Serialization.Bytes;

namespace Zerra.Encryption
{
    public sealed class Secret<T>
    {
        [NonSerialized]
        private static readonly SymmetricKey key = SymmetricEncryptor.GenerateKey(SymmetricAlgorithmType.AES);
        [NonSerialized]
        private readonly byte[] secretEncrypted;

        public Secret(T secret)
        {
            var bytes = ByteSerializer.Serialize(secret);
            this.secretEncrypted = SymmetricEncryptor.Encrypt(SymmetricAlgorithmType.AESwithShift, key, bytes);
        }

        private T? GetSecret()
        {
            var encryptedBytes = SymmetricEncryptor.Decrypt(SymmetricAlgorithmType.AESwithShift, key, secretEncrypted);
            var secret = ByteSerializer.Deserialize<T>(encryptedBytes);
            return secret;
        }

        public static bool operator ==(Secret<T> a, Secret<T> b)
        {
            if (a is null)
            {
                if (b is null)
                {
                    return true;
                }
                else
                {
                    if (b.GetSecret() is null)
                        return true;
                    else
                        return false;
                }
            }
            else if (b is null)
            {
                if (a.GetSecret() is null)
                    return true;
                else
                    return false;
            }

            var asecret = a.GetSecret();
            if (asecret is null)
            {
                if (b.GetSecret() is null)
                    return true;
                else
                    return false;
            }

            var bsecret = b.GetSecret();
            if (bsecret is null)
            {
                if (asecret is null)
                    return true;
                else
                    return false;
            }
            return asecret.Equals(bsecret);
        }
        public static bool operator !=(Secret<T> a, Secret<T> b)
        {
            if (a is null)
            {
                if (b is null)
                {
                    return false;
                }
                else
                {
                    if (b.GetSecret() is null)
                        return false;
                    else
                        return true;
                }
            }
            else if (b is null)
            {
                if (a.GetSecret() is null)
                    return false;
                else
                    return true;
            }

            var asecret = a.GetSecret();
            if (asecret is null)
            {
                if (b.GetSecret() is null)
                    return false;
                else
                    return true;
            }

            var bsecret = b.GetSecret();
            if (bsecret is null)
            {
                if (asecret is null)
                    return false;
                else
                    return true;
            }
            return !asecret.Equals(bsecret);
        }

        public static bool operator ==(Secret<T> a, T b)
        {
            if (a is null)
            {
                if (b is null)
                    return true;
                else
                    return false;
            }
            else if (b is null)
            {
                if (a.GetSecret() is null)
                    return true;
                else
                    return false;
            }
            var asecret = a.GetSecret();
            if (asecret is null)
                return false;
            return asecret.Equals(b);
        }
        public static bool operator !=(Secret<T> a, T b)
        {
            if (a is null)
            {
                if (b is null)
                    return false;
                else
                    return true;
            }
            else if (b is null)
            {
                if (a.GetSecret() is null)
                    return false;
                else
                    return true;
            }
            var asecret = a.GetSecret();
            if (asecret is null)
                return true;
            return !asecret.Equals(b);
        }

        public static implicit operator T?(Secret<T>? it)
        {
            if (it is null)
                return default;
            return it.GetSecret();
        }
        public static explicit operator Secret<T>?(T? it)
        {
            if (it is null)
                return null;
            return new Secret<T>(it);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(this, obj))
                return true;

            if (obj is null)
            {
                if (this.GetSecret() is null)
                    return true;
                else
                    return false;
            }

            if (obj is Secret<T> secretT)
            {
                var secretTsecret = secretT.GetSecret();
                var thissecret = this.GetSecret();
                if (secretTsecret is null)
                {
                    if (thissecret is null)
                        return true;
                    else
                        return false;
                }
                if (thissecret is null)
                    return false;
                return secretTsecret.Equals(thissecret);
            }

            if (obj is T t)
            {
                var thissecret = this.GetSecret();
                if (t is null)
                {
                    if (thissecret is null)
                        return true;
                    else
                        return false;
                }
                if (thissecret is null)
                    return false;
                return t.Equals(thissecret);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return secretEncrypted.GetHashCode();
        }

        public override string ToString()
        {
            return "[Secret]";
        }
    }
}
