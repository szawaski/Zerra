// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Serialization
{
    public static partial class JsonSerializer
    {
        private enum ReadFrameType : byte
        {
            Value,
            CoreType
        }
    }
}