// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections.Generic;

namespace Zerra.Test
{
    public partial class MapTest
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
        }
    }
}
