// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections.Concurrent;
using Zerra.Test.Helpers.Models;

namespace Zerra.Test.Helpers.TypesModels
{
    [Zerra.SourceGeneration.GenerateTypeDetail]
    public class TypesIDictionaryTOfTModel
    {
        public ConcurrentDictionary<int, string> DictionaryThing1 { get; set; }
        public ConcurrentDictionary<int, SimpleModel> DictionaryThing2 { get; set; }
        public ConcurrentDictionary<SimpleModel, int?> DictionaryThing3 { get; set; }
        public ConcurrentDictionary<string, string> DictionaryThing4 { get; set; }
        public ConcurrentDictionary<string, string> DictionaryThingEmpty { get; set; }
        public ConcurrentDictionary<string, string> DictionaryThingNull { get; set; }

        public static TypesIDictionaryTOfTModel Create()
        {
            var model = new TypesIDictionaryTOfTModel()
            {
                DictionaryThing1 = new ConcurrentDictionary<int, string>(new Dictionary<int, string>() { { 1, "A" }, { 2, "B" }, { 3, "C" }, { 4, null } }),
                DictionaryThing2 = new ConcurrentDictionary<int, SimpleModel>(new Dictionary<int, SimpleModel>() { { 1, new() { Value1 = 1, Value2 = "A" } }, { 2, new() { Value1 = 2, Value2 = "B" } }, { 3, new() { Value1 = 3, Value2 = "C" } }, { 4, null } }),
                DictionaryThing3 = new ConcurrentDictionary<SimpleModel, int?>(),
                DictionaryThing4 = new ConcurrentDictionary<string, string>(),
                DictionaryThingEmpty = new ConcurrentDictionary<string, string>(),
                DictionaryThingNull = null
            };
            _ = model.DictionaryThing3.TryAdd(new SimpleModel() { Value1 = 1, Value2 = "A" }, 1);
            _ = model.DictionaryThing3.TryAdd(new SimpleModel() { Value1 = 2, Value2 = "B" }, 2);
            _ = model.DictionaryThing3.TryAdd(new SimpleModel() { Value1 = 3, Value2 = "C" }, 3);
            _ = model.DictionaryThing3.TryAdd(new SimpleModel() { Value1 = 4, Value2 = "D" }, null);

            _ = model.DictionaryThing4.TryAdd("A", "1");
            _ = model.DictionaryThing4.TryAdd("B", "2");
            _ = model.DictionaryThing4.TryAdd("C", "3");
            _ = model.DictionaryThing4.TryAdd("D", null);

            return model;
        }
    }
}