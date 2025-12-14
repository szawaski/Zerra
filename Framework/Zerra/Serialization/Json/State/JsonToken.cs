// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Serialization.Json.State
{
    public enum JsonToken : byte
    {
        NotDetermined = 0,

        ObjectStart = 1,
        ObjectEnd = 2,
        ArrayStart = 3,
        ArrayEnd = 4,
        NextItem = 5,
        PropertySeperator = 6,

        Null= 7,
        False = 8,
        True = 9,
        String = 10,
        Number = 11,
    }
}