﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license


namespace Zerra.Serialization.Bytes
{
    public static partial class ByteSerializerOld
    {
        private readonly struct OptionsStruct
        {
            public readonly bool UsePropertyNames;
            public readonly bool IncludePropertyTypes;
            public readonly bool IgnoreIndexAttribute;
            public readonly ByteSerializerIndexSize IndexSize;

            public OptionsStruct(ByteSerializerOptions options)
            {
                this.UsePropertyNames = options.UsePropertyNames;
                this.IncludePropertyTypes = options.UseTypes;
                this.IgnoreIndexAttribute = options.IgnoreIndexAttribute;
                this.IndexSize = options.IndexSize;
            }
        }
    }
}