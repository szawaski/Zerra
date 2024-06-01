// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Serialization.Json
{
    public static partial class JsonSerializer
    {
        private enum ReadFrameType : byte
        {
            Value,
            StringToType,
            String,
            Object,
            Dictionary,
            Array,
            ArrayNameless,
            Literal,
            LiteralNumber
        }
    }
}