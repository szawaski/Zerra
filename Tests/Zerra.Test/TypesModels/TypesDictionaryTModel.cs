// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Zerra.Test
{
    public class TypesDictionaryTModel
    {
        public Dictionary<int, string> DictionaryThing1 { get; set; }
        public Dictionary<int, SimpleModel> DictionaryThing2 { get; set; }
        public IDictionary<int, string> DictionaryThing3 { get; set; }
        public IDictionary<int, SimpleModel> DictionaryThing4 { get; set; }
        public IReadOnlyDictionary<int, string> DictionaryThing5 { get; set; }
        public IReadOnlyDictionary<int, SimpleModel> DictionaryThing6 { get; set; }
        public ConcurrentDictionary<int, string> DictionaryThing7 { get; set; }
        public ConcurrentDictionary<int, SimpleModel> DictionaryThing8 { get; set; }

        public static TypesDictionaryTModel Create()
        {
            var model = new TypesDictionaryTModel()
            {
                DictionaryThing1 = new Dictionary<int, string>() { { 1, "A" }, { 2, "B" }, { 3, "C" }, { 4, null } },
                DictionaryThing2 = new Dictionary<int, SimpleModel>() { { 1, new() { Value1 = 1, Value2 = "A" } }, { 2, new() { Value1 = 2, Value2 = "B" } }, { 3, new() { Value1 = 3, Value2 = "C" } }, { 4, null } },
                DictionaryThing3 = new Dictionary<int, string>() { { 1, "A" }, { 2, "B" }, { 3, "C" }, { 4, null } },
                DictionaryThing4 = new Dictionary<int, SimpleModel>() { { 1, new() { Value1 = 1, Value2 = "A" } }, { 2, new() { Value1 = 2, Value2 = "B" } }, { 3, new() { Value1 = 3, Value2 = "C" } }, { 4, null } },
                DictionaryThing5 = new Dictionary<int, string>() { { 1, "A" }, { 2, "B" }, { 3, "C" }, { 4, null } },
                DictionaryThing6 = new Dictionary<int, SimpleModel>() { { 1, new() { Value1 = 1, Value2 = "A" } }, { 2, new() { Value1 = 2, Value2 = "B" } }, { 3, new() { Value1 = 3, Value2 = "C" } }, { 4, null } },
                DictionaryThing7 = new ConcurrentDictionary<int, string>(new Dictionary<int, string>() { { 1, "A" }, { 2, "B" }, { 3, "C" }, { 4, null } }),
                DictionaryThing8 = new ConcurrentDictionary<int, SimpleModel>(new Dictionary<int, SimpleModel>() { { 1, new() { Value1 = 1, Value2 = "A" } }, { 2, new() { Value1 = 2, Value2 = "B" } }, { 3, new() { Value1 = 3, Value2 = "C" } }, { 4, null } }),
            };
            return model;
        }
    }
}