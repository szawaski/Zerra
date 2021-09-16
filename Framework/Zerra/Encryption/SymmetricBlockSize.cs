// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Encryption
{
    public enum SymmetricBlockSize : short
    {
        Bits_128 = 128,
        Bits_192 = 192, //Rijndael Not Supported in .NetCore/.NetStandard
        Bits_256 = 256  //Rijndael Not Supported in .NetCore/.NetStandard
    }
}