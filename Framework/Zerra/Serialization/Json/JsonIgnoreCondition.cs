// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Serialization.Json
{
    public enum JsonIgnoreCondition : byte
    {
        Never = 0,
        Always = 1,
        WhenReading = 2,
        WhenWriting = 3,
        WhenWritingDefault = 4,
        WhenWritingNull = 5
    }
}