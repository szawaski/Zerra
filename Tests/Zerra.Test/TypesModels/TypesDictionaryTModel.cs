// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections.Generic;

namespace Zerra.Test
{
    public class TypesDictionaryTModel
    {
        public Dictionary<int, string> DictionaryThing1 { get; set; }
        public Dictionary<int, SimpleModel> DictionaryThing2 { get; set; }
        public Dictionary<SimpleModel, int?> DictionaryThing3 { get; set; }
        public Dictionary<string, string> DictionaryThing4 { get; set; }
        public Dictionary<string, string> DictionaryThingEmpty { get; set; }
        public Dictionary<string, string> DictionaryThingNull { get; set; }

        public static TypesDictionaryTModel Create()
        {
            var model = new TypesDictionaryTModel()
            {
                DictionaryThing1 = new Dictionary<int, string>() { { 1, "A" }, { 2, "B" }, { 3, "C" }, { 4, null } },
                DictionaryThing2 = new Dictionary<int, SimpleModel>() { { 1, new() { Value1 = 1, Value2 = "A" } }, { 2, new() { Value1 = 2, Value2 = "B" } }, { 3, new() { Value1 = 3, Value2 = "C" } }, { 4, null } },
                DictionaryThing3 = new Dictionary<SimpleModel, int?>() { { new() { Value1 = 1, Value2 = "A" }, 1 }, { new() { Value1 = 2, Value2 = "B" }, 2 }, { new() { Value1 = 3, Value2 = "C" }, 3 }, { new() { Value1 = 4, Value2 = "D" }, null } },
                DictionaryThing4 = new Dictionary<string, string>() { { "A", "1" }, { "B", "2" }, { "C", "3" }, { "D", null } },
                DictionaryThingEmpty = new Dictionary<string, string>(),
                DictionaryThingNull = null
            };
            return model;
        }
    }
}