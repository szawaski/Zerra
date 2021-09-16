// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Serialization;

namespace Zerra.Test
{
    public class TestSerializerIndexModel2
    {
        [SerializerIndex(2)]
        public int Value2 { get; set; }
        [SerializerIndex(3)]
        public int Value3 { get; set; }
        [SerializerIndex(1)]
        public int Value1 { get; set; }
    }
}
