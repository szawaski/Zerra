// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Serialization
{
    public sealed class JsonSerializerOptions
    {
        public bool Nameless { get; set; }
        public bool DoNotWriteNullProperties { get; set; }
        public bool EnumAsNumber { get; set; }
    }
}