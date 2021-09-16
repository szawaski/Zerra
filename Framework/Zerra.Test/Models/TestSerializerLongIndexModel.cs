// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Serialization;

namespace Zerra.Test
{
    public class TestSerializerLongIndexModel
    {
        [SerializerIndex(0)]
        public int Value1 { get; set; }
        [SerializerIndex(22334)]
        public int Value2 { get; set; }
        [SerializerIndex(ushort.MaxValue - ByteSerializer.IndexOffset)]
        public int Value3 { get; set; }
    }
}
