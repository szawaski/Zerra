// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Serialization.Json.State
{
    public enum ReadNumberStage : byte
    {
        Value = 0,
        ValueContinue = 1,
        Decimal = 2,
        Exponent = 3,
        ExponentContinue = 4
    }
}