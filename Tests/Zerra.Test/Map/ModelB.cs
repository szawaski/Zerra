// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections.Generic;
using Zerra.Encryption;

namespace Zerra.Test.Map
{
    public class ModelB
    {
        public Secret<int> Secret { get; set; }

        public int PropB { get; set; }
        public int PropD { get; set; }

        public int Prop1 { get; set; }
        public double Prop2 { get; set; }

        public int[] ArrayToArray { get; set; }
        public List<int> ArrayToList { get; set; }
        public IList<int> ArrayToIList { get; set; }
        public HashSet<int> ArrayToSet { get; set; }
        public ISet<int> ArrayToISet { get; set; }
        public ICollection<int> ArrayToICollection { get; set; }
        public IEnumerable<int> ArrayToIEnumerable { get; set; }

        public int[] ListToArray { get; set; }
        public List<int> ListToList { get; set; }
        public IList<int> ListToIList { get; set; }
        public HashSet<int> ListToSet { get; set; }
        public ISet<int> ListToISet { get; set; }
        public ICollection<int> ListToICollection { get; set; }
        public IEnumerable<int> ListToIEnumerable { get; set; }

        public int[] CollectionToArray { get; set; }
        public List<int> CollectionToList { get; set; }
        public IList<int> CollectionToIList { get; set; }
        public HashSet<int> CollectionToSet { get; set; }
        public ISet<int> CollectionToISet { get; set; }
        public ICollection<int> CollectionToICollection { get; set; }
        public IEnumerable<int> CollectionToIEnumerable { get; set; }

        public int[] EnumerableToArray { get; set; }
        public List<int> EnumerableToList { get; set; }
        public IList<int> EnumerableToIList { get; set; }
        public HashSet<int> EnumerableToSet { get; set; }
        public ISet<int> EnumerableToISet { get; set; }
        public ICollection<int> EnumerableToICollection { get; set; }
        public IEnumerable<int> EnumerableToIEnumerable { get; set; }

        public ModelB ModelToModel { get; set; }
        public ModelA[] ModelToModelArray { get; set; }

        public Dictionary<string, string> Dictionary1 { get; set; }
        public Dictionary<string, SimpleModel> Dictionary2 { get; set; }
        public IDictionary<string, string> DictionaryToIDiciontary { get; set; }
    }
}
