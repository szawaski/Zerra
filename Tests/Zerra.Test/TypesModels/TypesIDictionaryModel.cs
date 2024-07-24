// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections;
using System.Collections.Generic;

namespace Zerra.Test
{
    public class TypesIDictionaryModel
    {
        public IDictionary DictionaryThing1 { get; set; }
        public IDictionary DictionaryThing2 { get; set; }

        public static TypesIDictionaryModel Create()
        {
            var model = new TypesIDictionaryModel()
            {
                DictionaryThing1 = new Dictionary<int, string>() { { 1, "A" }, { 2, "B" }, { 3, "C" }, { 4, null } },
                DictionaryThing2 = new Dictionary<int, SimpleModel>() { { 1, new() { Value1 = 1, Value2 = "A" } }, { 2, new() { Value1 = 2, Value2 = "B" } }, { 3, new() { Value1 = 3, Value2 = "C" } }, { 4, null } },
            };
            return model;
        }
    }
}