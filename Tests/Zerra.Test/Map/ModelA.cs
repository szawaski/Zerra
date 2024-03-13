// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections.Generic;

namespace Zerra.Test.Map
{
    public class ModelA
    {
        public int PropA { get; set; }
        public int PropC { get; set; }

        public int Prop1 { get; set; }
        public int Prop2 { get; set; }

        public int[] ArrayToArray { get; set; }
        public int[] ArrayToList { get; set; }
        public int[] ArrayToIList { get; set; }
        public int[] ArrayToSet { get; set; }
        public int[] ArrayToISet { get; set; }
        public int[] ArrayToICollection { get; set; }
        public int[] ArrayToIEnumerable { get; set; }

        public List<int> ListToArray { get; set; }
        public List<int> ListToList { get; set; }
        public List<int> ListToIList { get; set; }
        public List<int> ListToSet { get; set; }
        public List<int> ListToISet { get; set; }
        public List<int> ListToICollection { get; set; }
        public List<int> ListToIEnumerable { get; set; }

        public ICollection<int> CollectionToArray { get; set; }
        public ICollection<int> CollectionToList { get; set; }
        public ICollection<int> CollectionToIList { get; set; }
        public ICollection<int> CollectionToSet { get; set; }
        public ICollection<int> CollectionToISet { get; set; }
        public ICollection<int> CollectionToICollection { get; set; }
        public ICollection<int> CollectionToIEnumerable { get; set; }

        public IEnumerable<int> EnumerableToArray { get; set; }
        public IEnumerable<int> EnumerableToList { get; set; }
        public IEnumerable<int> EnumerableToIList { get; set; }
        public IEnumerable<int> EnumerableToSet { get; set; }
        public IEnumerable<int> EnumerableToISet { get; set; }
        public IEnumerable<int> EnumerableToICollection { get; set; }
        public IEnumerable<int> EnumerableToIEnumerable { get; set; }

        public ModelA ModelToModel { get; set; }
        public ModelA[] ModelToModelArray { get; set; }

        public Dictionary<string, string> Dictionary1 { get; set; }
        public Dictionary<string, BasicModel> Dictionary2 { get; set; }
        public Dictionary<string, string> DictionaryToIDiciontary { get; set; }

        public static ModelA GetModelA()
        {
            return new ModelA()
            {
                PropA = 64,
                PropC = 128,

                Prop1 = 5,
                Prop2 = 15,

                ArrayToArray = new int[] { 4, 5, 6 },
                ArrayToList = new int[] { 7, 8, 9 },
                ArrayToIList = new int[] { 10, 11, 12 },
                ArrayToSet = new int[] { 13, 14, 15 },
                ArrayToISet = new int[] { 16, 17, 18 },
                ArrayToICollection = new int[] { 19, 20, 21 },
                ArrayToIEnumerable = new int[] { 22, 23, 24 },

                ListToArray = new List<int> { 4, 5, 6 },
                ListToList = new List<int> { 7, 8, 9 },
                ListToIList = new List<int> { 10, 11, 12 },
                ListToSet = new List<int> { 13, 14, 15 },
                ListToISet = new List<int> { 16, 17, 18 },
                ListToICollection = new List<int> { 19, 20, 21 },
                ListToIEnumerable = new List<int> { 22, 23, 24 },

                CollectionToArray = new List<int> { 4, 5, 6 },
                CollectionToList = new List<int> { 7, 8, 9 },
                CollectionToIList = new List<int> { 10, 11, 12 },
                CollectionToSet = new List<int> { 13, 14, 15 },
                CollectionToISet = new List<int> { 16, 17, 18 },
                CollectionToICollection = new List<int> { 19, 20, 21 },
                CollectionToIEnumerable = new List<int> { 22, 23, 24 },

                EnumerableToArray = new List<int> { 4, 5, 6 },
                EnumerableToList = new List<int> { 7, 8, 9 },
                EnumerableToIList = new List<int> { 10, 11, 12 },
                EnumerableToSet = new List<int> { 13, 14, 15 },
                EnumerableToISet = new List<int> { 16, 17, 18 },
                EnumerableToICollection = new List<int> { 19, 20, 21 },
                EnumerableToIEnumerable = new List<int> { 22, 23, 24 },

                ModelToModel = new ModelA()
                {
                    Prop1 = 101,
                    PropA = 102,
                    Prop2 = 103,
                    ArrayToArray = new int[] { 1, 2, 3 }
                },
                ModelToModelArray = new ModelA[]
                {
                    new()
                    {
                        Prop1 = 101,
                        PropA = 102,
                        Prop2 = 103,
                        ArrayToArray = new int[] { 1, 2, 3 },
                    },
                    new()
                    {
                        Prop1 = 104,
                        PropA = 105,
                        Prop2 = 106,
                        ArrayToArray = new int[] { 4, 5, 6 }
                    },
                    new()
                    {
                        Prop1 = 107,
                        PropA = 108,
                        Prop2 = 109,
                        ArrayToArray = new int[] { 7, 8, 9 }
                    }
                },

                Dictionary1 = new() { { "1", "A" }, { "2", "B" }, { "3", "C" } },
                Dictionary2 = new() { { "1", new() { Value1 = 1, Value2 = "A" } }, { "2", new() { Value1 = 2, Value2 = "B" } }, { "3", new() { Value1 = 3, Value2 = "C" } } },
                DictionaryToIDiciontary = new() { { "4", "D" }, { "5", "E" }, { "6", "F" } }
            };
        }
    }
}
