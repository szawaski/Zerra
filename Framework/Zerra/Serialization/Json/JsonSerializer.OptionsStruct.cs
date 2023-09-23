// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Serialization
{
    public static partial class JsonSerializer
    {
        private readonly struct OptionsStruct
        {
            public readonly bool Nameless;
            public readonly bool DoNotWriteNullProperties;

            public OptionsStruct(JsonSerializerOptions options)
            {
                this.Nameless = options.Nameless;
                this.DoNotWriteNullProperties = options.DoNotWriteNullProperties;
            }
        }
    }
}