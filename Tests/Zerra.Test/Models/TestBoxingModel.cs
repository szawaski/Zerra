// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Serialization.Bytes;

namespace Zerra.Test
{
    public class TestBoxingModel
    {
        [SerializerIndex(1)]
        public IBasicModel BoxedInterfaceThing { get; set; }
        [SerializerIndex(2)]
        public object BoxedObjectThing { get; set; }

        public static TestBoxingModel Create()
        {
            var model = new TestBoxingModel()
            {
                BoxedInterfaceThing = new SimpleModel() { Value1 = 10, Value2 = "S-10" },
                BoxedObjectThing = new SimpleModel() { Value1 = 11, Value2 = "S-11" }
            };
            return model;
        }
    }
}
