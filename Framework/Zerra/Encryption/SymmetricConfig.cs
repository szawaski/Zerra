// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Encryption
{
    /// <summary>
    /// A complete set of information for a symmetric encryption.
    /// This includes the algorithm and the key.
    /// </summary>
    public sealed class SymmetricConfig
    {
        /// <summary>
        /// The encryption algoritm.
        /// </summary>
        public SymmetricAlgorithmType Algorithm { get; }
        /// <summary>
        /// The encryption key.
        /// </summary>
        public SymmetricKey Key { get; }

        /// <summary>
        /// Creates a new instance of a SymmetricConfig
        /// </summary>
        /// <param name="algorithm">The encryption algorithm.</param>
        /// <param name="key">The encryption key.</param>
        public SymmetricConfig(SymmetricAlgorithmType algorithm, SymmetricKey key)
        {
            this.Algorithm = algorithm;
            this.Key = key;
        }
    }
}