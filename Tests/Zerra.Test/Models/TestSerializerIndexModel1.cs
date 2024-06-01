// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Serialization.Bytes;

namespace Zerra.Test
{
    public class TestSerializerIndexModel1
    {
        [SerializerIndex(1)]
        public int Value1 { get; set; }
        [SerializerIndex(2)]
        public int Value2 { get; set; }
        [SerializerIndex(3)]
        public int Value3 { get; set; }
    }
}
