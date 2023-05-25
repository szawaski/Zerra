// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Encryption
{
    public sealed class SymmetricConfig
    {
        public SymmetricAlgorithmType Algorithm { get; private set; }
        public SymmetricKey Key { get; private set; }
        
        public SymmetricConfig(SymmetricAlgorithmType algorithm, SymmetricKey key)
        {
            this.Algorithm = algorithm;
            this.Key = key;
        }
    }
}