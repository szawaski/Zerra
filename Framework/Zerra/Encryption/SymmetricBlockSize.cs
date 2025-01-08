// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Encryption
{
    /// <summary>
    /// The block size for a symmetric encryption algorithm.
    /// </summary>
    public enum SymmetricBlockSize : short
    {
        /// <summary>
        /// 128 bit block size.
        /// </summary>
        Bits_128 = 128,
        /// <summary>
        /// 192 bit block size.
        /// </summary>
        Bits_192 = 192, //Rijndael Not Supported in .NetCore/.NetStandard
        /// <summary>
        /// 256 bit block size.
        /// </summary>
        Bits_256 = 256  //Rijndael Not Supported in .NetCore/.NetStandard
    }
}