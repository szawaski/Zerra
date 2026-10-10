// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Security.Cryptography;

namespace Zerra.Encryption
{
    //the key check in the header isn't the key's
    internal sealed class KeyMismatchException : CryptographicException
    {
        public KeyMismatchException()
            : base("Encrypted data was changed or the key is wrong") { }
    }
}
