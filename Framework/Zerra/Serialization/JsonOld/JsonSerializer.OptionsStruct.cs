// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Serialization.Json
{
    public static partial class JsonSerializerOld
    {
        private readonly struct OptionsStruct
        {
            public readonly bool Nameless;
            public readonly bool DoNotWriteNullProperties;
            public readonly bool EnumAsNumber;

            public OptionsStruct(JsonSerializerOptions options)
            {
                this.Nameless = options.Nameless;
                this.DoNotWriteNullProperties = options.DoNotWriteNullProperties;
                this.EnumAsNumber = options.EnumAsNumber;
            }
        }
    }
}