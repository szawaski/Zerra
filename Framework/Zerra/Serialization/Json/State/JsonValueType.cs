// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Serialization.Json.State
{
    public enum JsonValueType : byte
    {
        NotDetermined = 0,
        ReadingInProgress = 1,

        Object = 2,
        Array = 3,
        String = 4,
        Number = 5,

        Null_Completed = 6,
        False_Completed = 7,
        True_Completed = 8
    }
}