// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Serialization.Json.State
{
    public enum JsonToken : byte
    {
        NotDetermined,

        ObjectStart,
        ObjectEnd,
        ArrayStart,
        ArrayEnd,
        NextItem,
        PropertySeperator,

        Null,
        False,
        True,
        String,
        Number
    }
}