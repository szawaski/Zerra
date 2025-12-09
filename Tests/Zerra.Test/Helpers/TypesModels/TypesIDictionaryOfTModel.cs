// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using Zerra.Test.Helpers.Models;

namespace Zerra.Test.Helpers.TypesModels
{
    [Zerra.Reflection.GenerateTypeDetail]
    public class TypesIDictionaryOfTModel
    {
        public CustomIDictionary DictionaryThing1 { get; set; }
        public CustomIDictionary DictionaryThing2 { get; set; }
        public CustomIDictionary DictionaryThing3 { get; set; }
        public CustomIDictionary DictionaryThing4 { get; set; }
        public CustomIDictionary DictionaryThingEmpty { get; set; }
        public CustomIDictionary DictionaryThingNull { get; set; }

        public sealed class CustomIDictionary : IDictionary
        {
            private readonly Dictionary<object, object> dictionary;
            public CustomIDictionary()
            {
                dictionary = new Dictionary<object, object>();
            }

            public object this[object key] { get => dictionary[key]; set => dictionary[key] = value; }

            public bool IsFixedSize => false;
            public bool IsReadOnly => false;
            public ICollection Keys => dictionary.Keys;
            public ICollection Values => dictionary.Values;
            public int Count => dictionary.Count;

            public bool IsSynchronized => ((IDictionary)dictionary).IsSynchronized;

            public object SyncRoot => ((IDictionary)dictionary).SyncRoot;

            public void Add(object key, object value) => ((IDictionary)dictionary).Add(key, value);
            public void Clear() => ((IDictionary)dictionary).Clear();
            public bool Contains(object key) => ((IDictionary)dictionary).Contains(key);
            public void CopyTo(Array array, int index) => ((IDictionary)dictionary).CopyTo(array, index);
            public IDictionaryEnumerator GetEnumerator() => ((IDictionary)dictionary).GetEnumerator();
            public void Remove(object key) => ((IDictionary)dictionary).Remove(key);
            IEnumerator IEnumerable.GetEnumerator() => ((IDictionary)dictionary).GetEnumerator();
        }

        public static TypesIDictionaryOfTModel Create()
        {
            var model = new TypesIDictionaryOfTModel()
            {
                DictionaryThing1 = new CustomIDictionary() { { 1, "A" }, { 2, "B" }, { 3, "C" }, { 4, null } },
                DictionaryThing2 = new CustomIDictionary() { { 1, new SimpleModel() { Value1 = 1, Value2 = "A" } }, { 2, new SimpleModel() { Value1 = 2, Value2 = "B" } }, { 3, new SimpleModel() { Value1 = 3, Value2 = "C" } }, { 4, null } },
                DictionaryThing3 = new CustomIDictionary() { { new SimpleModel() { Value1 = 1, Value2 = "A" }, 1 }, { new SimpleModel() { Value1 = 2, Value2 = "B" }, 2 }, { new SimpleModel() { Value1 = 3, Value2 = "C" }, 3 }, { new SimpleModel() { Value1 = 4, Value2 = "D" }, null } },
                DictionaryThing4 = new CustomIDictionary() { { "A", "1" }, { "B", "2" }, { "C", "3" }, { "D", null } },
                DictionaryThingEmpty = new CustomIDictionary(),
                DictionaryThingNull = null
            };
            return model;
        }
    }
}