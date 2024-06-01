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
    }
}
