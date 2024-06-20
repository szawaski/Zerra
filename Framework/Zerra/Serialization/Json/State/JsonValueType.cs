// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Serialization.Json.State
{
    public enum JsonValueType
    {
        NotDetermined,
        ReadingInProgress,

        Object_Started,
        Array_Started,
        String_Started,
        Number_Started,

        Null_Completed,
        False_Completed,
        True_Completed,
    }
}