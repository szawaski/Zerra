// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Encryption
{
    public sealed class AsymmetricKeyPair
    {
        public string PublicKey { get; private set; }
        public string PrivateKey { get; private set; }
        public AsymmetricKeyPair(string publicKey, string privateKey)
        {
            this.PublicKey = publicKey;
            this.PrivateKey = privateKey;
        }
    }
}