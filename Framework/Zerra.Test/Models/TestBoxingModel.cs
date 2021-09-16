// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Serialization;

namespace Zerra.Test
{
    public class TestBoxingModel
    {
        [SerializerIndex(1)]
        public IBasicModel BoxedThing { get; set; }
    }
}
