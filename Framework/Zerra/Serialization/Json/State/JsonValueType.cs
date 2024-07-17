// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Serialization.Json.State
{
    public enum JsonValueType : byte
    {
        NotDetermined = 0,

        Object = 1,
        Array = 2,
        String = 3,
        Number = 4,

        Null_Completed = 5,
        False_Completed = 6,
        True_Completed = 7
    }
}