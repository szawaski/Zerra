// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Encryption
{
    public enum SymmetricAlgorithmType : byte
    {
        AES,
        DES,
        TripleDES,
        RC2,

        AESwithShift,
        DESwithShift,
        TripleDESwithShift,
        RC2withShift
    }
}