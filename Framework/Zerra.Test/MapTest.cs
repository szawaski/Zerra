// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Zerra.Test
{
    public class MapperLog : IMapLogger
    {
        private readonly List<string> log = new();

        public void LogPropertyChange(string source, string sourceValue, string target, string targetValue)
        {
            log.Add($"Changed {target} From {targetValue} To {sourceValue} Source {source}");
        }

        public void LogPropertyNoChange(string source, string target, string targetValue)
        {
            log.Add($"No Change From {target} To {source}");
        }

        public void LogNewObject(string source, string target, string type)
        {
            log.Add($"New {type} On {target}");
        }
    }

    [TestClass]
    public partial class MapTest
    {
        private static ModelA GetModelA()
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
                    new ModelA()
                    {
                        Prop1 = 101,
                        PropA = 102,
                        Prop2 = 103,
                        ArrayToArray = new int[] { 1, 2, 3 },
                    },
                    new ModelA()
                    {
                        Prop1 = 104,
                        PropA = 105,
                        Prop2 = 106,
                        ArrayToArray = new int[] { 4, 5, 6 }
                    },
                    new ModelA()
                    {
                        Prop1 = 107,
                        PropA = 108,
                        Prop2 = 109,
                        ArrayToArray = new int[] { 7, 8, 9 }
                    }
                }
            };
        }

        private struct Test
        {
            public double P1 { get; set; }
            public double P2 { get; set; }
        }

        [TestMethod]
        public void ValueTypes()
        {
            var value1 = 5;
            var result1 = value1.Copy();
            Assert.AreEqual(value1, result1);

            var value2 = new Test { P1 = 1, P2 = 2 };
            var result2 = value2.Copy();
            Assert.AreEqual(value2.P1, result2.P1);
            Assert.AreEqual(value2.P2, result2.P2);
        }
        [TestMethod]
        public void ValueTypesLogger()
        {
            var log = new MapperLog();
            var value1 = 5;
            var result1 = value1.Copy(log);
            Assert.AreEqual(value1, result1);

            var value2 = new Test { P1 = 1, P2 = 2 };
            var result2 = value2.Copy(log);
            Assert.AreEqual(value2.P1, result2.P1);
            Assert.AreEqual(value2.P2, result2.P2);
        }

        [TestMethod]
        public void ComplexTypes()
        {
            var modelA = GetModelA();
            var modelB = modelA.Map<ModelB>();

            Assert.IsNotNull(modelB);
            Assert.AreEqual(modelA.Prop1, modelB.Prop1);
            Assert.AreEqual(modelA.Prop2, modelB.Prop2);

            Assert.AreEqual(modelA.ArrayToArray.Length, modelB.ArrayToArray.Length);
            Assert.AreEqual(modelA.ArrayToArray[0], modelB.ArrayToArray[0]);
            Assert.AreEqual(modelA.ArrayToArray[1], modelB.ArrayToArray[1]);
            Assert.AreEqual(modelA.ArrayToArray[2], modelB.ArrayToArray[2]);

            Assert.AreEqual(modelA.ArrayToList.Length, modelB.ArrayToList.Count);
            Assert.AreEqual(modelA.ArrayToList[0], modelB.ArrayToList[0]);
            Assert.AreEqual(modelA.ArrayToList[1], modelB.ArrayToList[1]);
            Assert.AreEqual(modelA.ArrayToList[2], modelB.ArrayToList[2]);

            Assert.AreEqual(modelA.ArrayToIList.Length, modelB.ArrayToIList.Count);
            Assert.AreEqual(modelA.ArrayToIList[0], modelB.ArrayToIList[0]);
            Assert.AreEqual(modelA.ArrayToIList[1], modelB.ArrayToIList[1]);
            Assert.AreEqual(modelA.ArrayToIList[2], modelB.ArrayToIList[2]);

            Assert.AreEqual(modelA.ArrayToSet.Length, modelB.ArrayToSet.Count);
            Assert.IsTrue(modelB.ArrayToSet.Contains(modelA.ArrayToSet[0]));
            Assert.IsTrue(modelB.ArrayToSet.Contains(modelA.ArrayToSet[1]));
            Assert.IsTrue(modelB.ArrayToSet.Contains(modelA.ArrayToSet[2]));

            Assert.AreEqual(modelA.ArrayToISet.Length, modelB.ArrayToISet.Count);
            Assert.IsTrue(modelB.ArrayToISet.Contains(modelA.ArrayToISet[0]));
            Assert.IsTrue(modelB.ArrayToISet.Contains(modelA.ArrayToISet[1]));
            Assert.IsTrue(modelB.ArrayToISet.Contains(modelA.ArrayToISet[2]));

            Assert.AreEqual(modelA.ArrayToICollection.Length, modelB.ArrayToICollection.Count);
            Assert.IsTrue(modelB.ArrayToICollection.Contains(modelA.ArrayToICollection[0]));
            Assert.IsTrue(modelB.ArrayToICollection.Contains(modelA.ArrayToICollection[1]));
            Assert.IsTrue(modelB.ArrayToICollection.Contains(modelA.ArrayToICollection[2]));

            Assert.AreEqual(modelA.ArrayToIEnumerable.Length, modelB.ArrayToIEnumerable.Count());
            Assert.IsTrue(modelB.ArrayToIEnumerable.Any(x => x == modelA.ArrayToIEnumerable[0]));
            Assert.IsTrue(modelB.ArrayToIEnumerable.Any(x => x == modelA.ArrayToIEnumerable[1]));
            Assert.IsTrue(modelB.ArrayToIEnumerable.Any(x => x == modelA.ArrayToIEnumerable[2]));

            Assert.AreEqual(modelA.ListToArray.Count, modelB.ListToArray.Length);
            Assert.AreEqual(modelA.ListToArray[0], modelB.ListToArray[0]);
            Assert.AreEqual(modelA.ListToArray[1], modelB.ListToArray[1]);
            Assert.AreEqual(modelA.ListToArray[2], modelB.ListToArray[2]);

            Assert.AreEqual(modelA.ListToList.Count, modelB.ListToList.Count);
            Assert.AreEqual(modelA.ListToList[0], modelB.ListToList[0]);
            Assert.AreEqual(modelA.ListToList[1], modelB.ListToList[1]);
            Assert.AreEqual(modelA.ListToList[2], modelB.ListToList[2]);

            Assert.AreEqual(modelA.ListToIList.Count, modelB.ListToIList.Count);
            Assert.AreEqual(modelA.ListToIList[0], modelB.ListToIList[0]);
            Assert.AreEqual(modelA.ListToIList[1], modelB.ListToIList[1]);
            Assert.AreEqual(modelA.ListToIList[2], modelB.ListToIList[2]);

            Assert.AreEqual(modelA.ListToSet.Count, modelB.ListToSet.Count);
            Assert.IsTrue(modelB.ListToSet.Contains(modelA.ListToSet[0]));
            Assert.IsTrue(modelB.ListToSet.Contains(modelA.ListToSet[1]));
            Assert.IsTrue(modelB.ListToSet.Contains(modelA.ListToSet[2]));

            Assert.AreEqual(modelA.ListToISet.Count, modelB.ListToISet.Count);
            Assert.IsTrue(modelB.ListToISet.Contains(modelA.ListToISet[0]));
            Assert.IsTrue(modelB.ListToISet.Contains(modelA.ListToISet[1]));
            Assert.IsTrue(modelB.ListToISet.Contains(modelA.ListToISet[2]));

            Assert.AreEqual(modelA.ListToICollection.Count, modelB.ListToICollection.Count);
            Assert.IsTrue(modelB.ListToICollection.Contains(modelA.ListToICollection[0]));
            Assert.IsTrue(modelB.ListToICollection.Contains(modelA.ListToICollection[1]));
            Assert.IsTrue(modelB.ListToICollection.Contains(modelA.ListToICollection[2]));

            Assert.AreEqual(modelA.ListToIEnumerable.Count, modelB.ListToIEnumerable.Count());
            Assert.IsTrue(modelB.ListToIEnumerable.Any(x => x == modelA.ListToIEnumerable[0]));
            Assert.IsTrue(modelB.ListToIEnumerable.Any(x => x == modelA.ListToIEnumerable[1]));
            Assert.IsTrue(modelB.ListToIEnumerable.Any(x => x == modelA.ListToIEnumerable[2]));
        }
        [TestMethod]
        public void ComplexTypesLogger()
        {
            var log = new MapperLog();
            var modelA = GetModelA();
            var modelB = modelA.Map<ModelB>(log);

            Assert.IsNotNull(modelB);
            Assert.AreEqual(modelA.Prop1, modelB.Prop1);
            Assert.AreEqual(modelA.Prop2, modelB.Prop2);

            Assert.AreEqual(modelA.ArrayToArray.Length, modelB.ArrayToArray.Length);
            Assert.AreEqual(modelA.ArrayToArray[0], modelB.ArrayToArray[0]);
            Assert.AreEqual(modelA.ArrayToArray[1], modelB.ArrayToArray[1]);
            Assert.AreEqual(modelA.ArrayToArray[2], modelB.ArrayToArray[2]);

            Assert.AreEqual(modelA.ArrayToList.Length, modelB.ArrayToList.Count);
            Assert.AreEqual(modelA.ArrayToList[0], modelB.ArrayToList[0]);
            Assert.AreEqual(modelA.ArrayToList[1], modelB.ArrayToList[1]);
            Assert.AreEqual(modelA.ArrayToList[2], modelB.ArrayToList[2]);

            Assert.AreEqual(modelA.ArrayToIList.Length, modelB.ArrayToIList.Count);
            Assert.AreEqual(modelA.ArrayToIList[0], modelB.ArrayToIList[0]);
            Assert.AreEqual(modelA.ArrayToIList[1], modelB.ArrayToIList[1]);
            Assert.AreEqual(modelA.ArrayToIList[2], modelB.ArrayToIList[2]);

            Assert.AreEqual(modelA.ArrayToSet.Length, modelB.ArrayToSet.Count);
            Assert.IsTrue(modelB.ArrayToSet.Contains(modelA.ArrayToSet[0]));
            Assert.IsTrue(modelB.ArrayToSet.Contains(modelA.ArrayToSet[1]));
            Assert.IsTrue(modelB.ArrayToSet.Contains(modelA.ArrayToSet[2]));

            Assert.AreEqual(modelA.ArrayToISet.Length, modelB.ArrayToISet.Count);
            Assert.IsTrue(modelB.ArrayToISet.Contains(modelA.ArrayToISet[0]));
            Assert.IsTrue(modelB.ArrayToISet.Contains(modelA.ArrayToISet[1]));
            Assert.IsTrue(modelB.ArrayToISet.Contains(modelA.ArrayToISet[2]));

            Assert.AreEqual(modelA.ArrayToICollection.Length, modelB.ArrayToICollection.Count);
            Assert.IsTrue(modelB.ArrayToICollection.Contains(modelA.ArrayToICollection[0]));
            Assert.IsTrue(modelB.ArrayToICollection.Contains(modelA.ArrayToICollection[1]));
            Assert.IsTrue(modelB.ArrayToICollection.Contains(modelA.ArrayToICollection[2]));

            Assert.AreEqual(modelA.ArrayToIEnumerable.Length, modelB.ArrayToIEnumerable.Count());
            Assert.IsTrue(modelB.ArrayToIEnumerable.Any(x => x == modelA.ArrayToIEnumerable[0]));
            Assert.IsTrue(modelB.ArrayToIEnumerable.Any(x => x == modelA.ArrayToIEnumerable[1]));
            Assert.IsTrue(modelB.ArrayToIEnumerable.Any(x => x == modelA.ArrayToIEnumerable[2]));

            Assert.AreEqual(modelA.ListToArray.Count, modelB.ListToArray.Length);
            Assert.AreEqual(modelA.ListToArray[0], modelB.ListToArray[0]);
            Assert.AreEqual(modelA.ListToArray[1], modelB.ListToArray[1]);
            Assert.AreEqual(modelA.ListToArray[2], modelB.ListToArray[2]);

            Assert.AreEqual(modelA.ListToList.Count, modelB.ListToList.Count);
            Assert.AreEqual(modelA.ListToList[0], modelB.ListToList[0]);
            Assert.AreEqual(modelA.ListToList[1], modelB.ListToList[1]);
            Assert.AreEqual(modelA.ListToList[2], modelB.ListToList[2]);

            Assert.AreEqual(modelA.ListToIList.Count, modelB.ListToIList.Count);
            Assert.AreEqual(modelA.ListToIList[0], modelB.ListToIList[0]);
            Assert.AreEqual(modelA.ListToIList[1], modelB.ListToIList[1]);
            Assert.AreEqual(modelA.ListToIList[2], modelB.ListToIList[2]);

            Assert.AreEqual(modelA.ListToSet.Count, modelB.ListToSet.Count);
            Assert.IsTrue(modelB.ListToSet.Contains(modelA.ListToSet[0]));
            Assert.IsTrue(modelB.ListToSet.Contains(modelA.ListToSet[1]));
            Assert.IsTrue(modelB.ListToSet.Contains(modelA.ListToSet[2]));

            Assert.AreEqual(modelA.ListToISet.Count, modelB.ListToISet.Count);
            Assert.IsTrue(modelB.ListToISet.Contains(modelA.ListToISet[0]));
            Assert.IsTrue(modelB.ListToISet.Contains(modelA.ListToISet[1]));
            Assert.IsTrue(modelB.ListToISet.Contains(modelA.ListToISet[2]));

            Assert.AreEqual(modelA.ListToICollection.Count, modelB.ListToICollection.Count);
            Assert.IsTrue(modelB.ListToICollection.Contains(modelA.ListToICollection[0]));
            Assert.IsTrue(modelB.ListToICollection.Contains(modelA.ListToICollection[1]));
            Assert.IsTrue(modelB.ListToICollection.Contains(modelA.ListToICollection[2]));

            Assert.AreEqual(modelA.ListToIEnumerable.Count, modelB.ListToIEnumerable.Count());
            Assert.IsTrue(modelB.ListToIEnumerable.Any(x => x == modelA.ListToIEnumerable[0]));
            Assert.IsTrue(modelB.ListToIEnumerable.Any(x => x == modelA.ListToIEnumerable[1]));
            Assert.IsTrue(modelB.ListToIEnumerable.Any(x => x == modelA.ListToIEnumerable[2]));
        }

        [TestMethod]
        public void ArrayType()
        {
            var modelA = new ModelA[]
            {
                GetModelA(),
                GetModelA(),
                GetModelA()
            };

            var modelB = modelA.Map<ModelB[]>();
            Assert.IsNotNull(modelB);
            Assert.AreEqual(modelA.Length, modelB.Length);
        }
        [TestMethod]
        public void ArrayTypeLogger()
        {
            var log = new MapperLog();
            var modelA = new ModelA[]
            {
                GetModelA(),
                GetModelA(),
                GetModelA()
            };

            var modelB = modelA.Map<ModelB[]>(log);
            Assert.IsNotNull(modelB);
            Assert.AreEqual(modelA.Length, modelB.Length);
        }

        public class DefineModelAToModelB : IMapDefinition<ModelA, ModelB>
        {
            public void Define(IMapSetup<ModelA, ModelB> map)
            {
                map.Define(x => x.PropB, x => Int32.Parse(x.PropA.ToString() + "1"));
                map.DefineTwoWay(x => x.PropD, x => x.PropC);
            }
        }

        [TestMethod]
        public void Define()
        {
            var modelA = GetModelA();
            var modelB = modelA.Map<ModelA, ModelB>();
            Assert.AreEqual(Int32.Parse(modelA.PropA.ToString() + "1"), modelB.PropB);
            Assert.AreEqual(modelA.PropC, modelB.PropD);

            modelA = modelB.Map<ModelB, ModelA>();
            Assert.AreEqual(default, modelA.PropA);
            Assert.AreEqual(modelA.PropC, modelB.PropD);
        }
        [TestMethod]
        public void DefineLogger()
        {
            var log = new MapperLog();
            var modelA = GetModelA();
            var modelB = modelA.Map<ModelA, ModelB>(log);
            Assert.AreEqual(Int32.Parse(modelA.PropA.ToString() + "1"), modelB.PropB);
            Assert.AreEqual(modelA.PropC, modelB.PropD);

            modelA = modelB.Map<ModelB, ModelA>(log);
            Assert.AreEqual(default, modelA.PropA);
            Assert.AreEqual(modelA.PropC, modelB.PropD);
        }

        [TestMethod]
        public void Graph()
        {
            var modelA = GetModelA();
            var modelB = modelA.Map<ModelA, ModelB>(new Graph<ModelB>(
                x => x.Prop1,
                x => x.ArrayToList
            ));
            Assert.IsNotNull(modelB);
            Assert.AreEqual(modelA.Prop1, modelB.Prop1);
            Assert.AreEqual(0, modelB.Prop2);
            Assert.IsNotNull(modelB.ArrayToList);
            Assert.AreEqual(modelA.ArrayToArray.Length, modelB.ArrayToList.Count);
        }
        [TestMethod]
        public void GraphLogger()
        {
            var log = new MapperLog();
            var modelA = GetModelA();
            var modelB = modelA.Map<ModelA, ModelB>(log, new Graph<ModelB>(
                x => x.Prop1,
                x => x.ArrayToList
            ));
            Assert.IsNotNull(modelB);
            Assert.AreEqual(modelA.Prop1, modelB.Prop1);
            Assert.AreEqual(0, modelB.Prop2);
            Assert.IsNotNull(modelB.ArrayToList);
            Assert.AreEqual(modelA.ArrayToArray.Length, modelB.ArrayToList.Count);
        }

        [TestMethod]
        public void Null()
        {
            var modelB = Mapper.Map<ModelB>(null);
            Assert.IsNull(modelB);
        }
        [TestMethod]
        public void NullLogger()
        {
            var log = new MapperLog();
            var modelB = MapperWithLog.Map<ModelB>(null, log);
            Assert.IsNull(modelB);
        }

        [TestMethod]
        public void Collections()
        {
            var modelAs = new ModelA[] { GetModelA(), GetModelA(), GetModelA() };

            var modelBs = modelAs.Map<ICollection<ModelB>>();
            var modelCs = modelAs.Select(x => x).Map<ICollection<ModelB>>();
            var modelDs = modelAs.ToList().Map<ICollection<ModelB>>();
            var modelEs = modelAs.ToHashSet().Map<ICollection<ModelB>>();

            var modelFs = modelAs.Map<IList<ModelB>>();
            var modelGs = modelAs.Map<ISet<ModelB>>();
            var modelHs = modelAs.Map<IEnumerable<ModelB>>();
            var modelIs = modelAs.Map<ModelB[]>();

            var modelJs = modelAs.Select(x => x).Map<IList<ModelB>>();
            var modelKs = modelAs.Select(x => x).Map<ISet<ModelB>>();
            var modelLs = modelAs.Select(x => x).Map<IEnumerable<ModelB>>();
            var modelMs = modelAs.Select(x => x).Map<ModelB[]>();

            Assert.AreEqual(typeof(ModelB[]), modelBs.GetType());
            Assert.AreEqual(typeof(List<ModelB>), modelCs.GetType());
            Assert.AreEqual(typeof(ModelB[]), modelDs.GetType());
            Assert.AreEqual(typeof(ModelB[]), modelEs.GetType());

            Assert.AreEqual(typeof(List<ModelB>), modelFs.GetType());
            Assert.AreEqual(typeof(HashSet<ModelB>), modelGs.GetType());
            Assert.AreEqual(typeof(ModelB[]), modelHs.GetType());
            Assert.AreEqual(typeof(ModelB[]), modelIs.GetType());

            Assert.AreEqual(typeof(List<ModelB>), modelJs.GetType());
            Assert.AreEqual(typeof(HashSet<ModelB>), modelKs.GetType());
            Assert.AreEqual(typeof(List<ModelB>), modelLs.GetType());
            Assert.AreEqual(typeof(ModelB[]), modelMs.GetType());
        }
        [TestMethod]
        public void CollectionsLogger()
        {
            var log = new MapperLog();
            var modelAs = new ModelA[] { GetModelA(), GetModelA(), GetModelA() };

            var modelBs = modelAs.Map<ICollection<ModelB>>(log);
            var modelCs = modelAs.Select(x => x).Map<ICollection<ModelB>>();
            var modelDs = modelAs.ToList().Map<ICollection<ModelB>>();
            var modelEs = modelAs.ToHashSet().Map<ICollection<ModelB>>();

            var modelFs = modelAs.Map<IList<ModelB>>();
            var modelGs = modelAs.Map<ISet<ModelB>>();
            var modelHs = modelAs.Map<IEnumerable<ModelB>>();
            var modelIs = modelAs.Map<ModelB[]>();

            var modelJs = modelAs.Select(x => x).Map<IList<ModelB>>();
            var modelKs = modelAs.Select(x => x).Map<ISet<ModelB>>();
            var modelLs = modelAs.Select(x => x).Map<IEnumerable<ModelB>>();
            var modelMs = modelAs.Select(x => x).Map<ModelB[]>();

            Assert.AreEqual(typeof(ModelB[]), modelBs.GetType());
            Assert.AreEqual(typeof(List<ModelB>), modelCs.GetType());
            Assert.AreEqual(typeof(ModelB[]), modelDs.GetType());
            Assert.AreEqual(typeof(ModelB[]), modelEs.GetType());

            Assert.AreEqual(typeof(List<ModelB>), modelFs.GetType());
            Assert.AreEqual(typeof(HashSet<ModelB>), modelGs.GetType());
            Assert.AreEqual(typeof(ModelB[]), modelHs.GetType());
            Assert.AreEqual(typeof(ModelB[]), modelIs.GetType());

            Assert.AreEqual(typeof(List<ModelB>), modelJs.GetType());
            Assert.AreEqual(typeof(HashSet<ModelB>), modelKs.GetType());
            Assert.AreEqual(typeof(List<ModelB>), modelLs.GetType());
            Assert.AreEqual(typeof(ModelB[]), modelMs.GetType());
        }

        public class TA
        {
            public int ID { get; set; }
            public TA Prop1 { get; set; }
            public TA Prop2 { get; set; }
        }

        public class TB
        {
            public int ID { get; set; }
            public TB Prop1 { get; set; }
            public TC Prop2 { get; set; }
        }

        public class TC
        {
            public int ID { get; set; }
            public TB Prop1 { get; set; }
            public TB Prop2 { get; set; }
        }

        [TestMethod]
        public void Recursion()
        {
            var modelA = new TA() { ID = 1 };
            modelA.Prop1 = modelA;
            modelA.Prop2 = modelA;

            var result = modelA.Map<TB>();
        }
        [TestMethod]
        public void RecursionLogger()
        {
            var log = new MapperLog();
            var modelA = new TA() { ID = 1 };
            modelA.Prop1 = modelA;
            modelA.Prop2 = modelA;

            var result = modelA.Map<TB>(log);
        }

        [TestMethod]
        public void CastedType()
        {
            var modelA = new ModelA[]
            {
                GetModelA(),
                GetModelA(),
                GetModelA()
            };

            var modelB = modelA.Map<ICollection<ModelB>>();
            Assert.IsNotNull(modelB);
            Assert.AreEqual(modelA.Length, modelB.Count);
        }
        [TestMethod]
        public void CastedTypeLogger()
        {
            var log = new MapperLog();
            var modelA = new ModelA[]
            {
                GetModelA(),
                GetModelA(),
                GetModelA()
            };

            var modelB = modelA.Map<ICollection<ModelB>>(log);
            Assert.IsNotNull(modelB);
            Assert.AreEqual(modelA.Length, modelB.Count);
        }

        public struct TestDebug1
        {
            public string Prop { get; set; }
        }

        public struct TestDebug2
        {
            public string Prop { get; set; }
        }

        public class DefineTestDebug1ToTestDebug2 : IMapDefinition<TestDebug1, TestDebug2>
        {
            public void Define(IMapSetup<TestDebug1, TestDebug2> map)
            {
                map.Define(x => x.Prop, x => x.Prop.Remove(10));
            }
        }

        [TestMethod]
        public void StringConversions()
        {
            var model = Factory.GetCoreTypesModel();

            var modelStrings = model.Map<CoreTypesAsStringsModel>();
            Factory.AssertAreEqual(model, modelStrings);

            model = modelStrings.Map<CoreTypesModel>();
            Factory.AssertAreEqual(model, modelStrings);
        }
    }
}
