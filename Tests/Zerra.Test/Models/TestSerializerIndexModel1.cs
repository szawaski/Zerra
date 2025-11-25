// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
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

        public static TestSerializerIndexModel1 Create()
        {
            return new TestSerializerIndexModel1()
            {
                Value1 = 11,
                Value2 = 22,
                Value3 = 33
            };
        }

        public static void AssertAreNotEqual(TestSerializerIndexModel1 model1, TestSerializerIndexModel2 model2)
        {
            Assert.NotNull(model1);
            Assert.NotNull(model2);
            Assert.NotEqual<object>(model1, model2);

            Assert.NotEqual(model1.Value1, model2.Value1);
            Assert.NotEqual(model1.Value2, model2.Value2);
            Assert.NotEqual(model1.Value3, model2.Value3);
        }
    }
}
