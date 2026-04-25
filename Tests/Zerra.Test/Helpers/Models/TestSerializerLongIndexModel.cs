// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license


// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Serialization.Bytes;

namespace Zerra.Test.Helpers.Models
{
    [Zerra.Reflection.GenerateTypeDetail]
    public class TestSerializerLongIndexModel
    {
        [SerializerIndex(0)]
        public int Value1 { get; set; }
        [SerializerIndex(22334)]
        public int Value2 { get; set; }
        [SerializerIndex(System.UInt16.MaxValue - 1)] //minus 1 index offset
        public int Value3 { get; set; }

        public static TestSerializerLongIndexModel Create()
        {
            return new TestSerializerLongIndexModel()
            {
                Value1 = 11,
                Value2 = 22,
                Value3 = 33
            };
        }
    }
}
