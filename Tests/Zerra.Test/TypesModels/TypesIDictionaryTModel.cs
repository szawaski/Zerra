// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections.Generic;

namespace Zerra.Test
{
    public class TypesIDictionaryTModel
    {
        public IDictionary<int, string> DictionaryThing3 { get; set; }
        public IDictionary<int, SimpleModel> DictionaryThing4 { get; set; }
        public static TypesIDictionaryTModel Create()
        {
            var model = new TypesIDictionaryTModel()
            {
                DictionaryThing3 = new Dictionary<int, string>() { { 1, "A" }, { 2, "B" }, { 3, "C" }, { 4, null } },
                DictionaryThing4 = new Dictionary<int, SimpleModel>() { { 1, new() { Value1 = 1, Value2 = "A" } }, { 2, new() { Value1 = 2, Value2 = "B" } }, { 3, new() { Value1 = 3, Value2 = "C" } }, { 4, null } },
            };
            return model;
        }
    }
}