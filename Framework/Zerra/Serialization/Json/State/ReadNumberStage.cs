// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Serialization.Json.State
{
    public enum ReadNumberStage : byte
    {
        Setup = 0,
        Value = 1,
        ValueContinue = 2,
        Decimal = 3,
        Exponent = 4,
        ExponentContinue = 5
    }
}