// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using Zerra.Map;
using Zerra.Reflection;
using Zerra.Test.Map;

namespace Zerra.Test
{
    public class MapTest
    {
        [Fact]
        public void StringTypes()
        {
            var value1 = "Test";
            var result1 = value1.Copy();
            Assert.Equal(value1, result1);
        }
        [Fact]
        public void StringTypesLogger()
        {
            var log = new MapperLog();

            var value1 = "Test";
            var result1 = value1.Copy(log);
            Assert.Equal(value1, result1);
        }

        [Fact]
        public void NumberTypes()
        {
            var value1 = 12345.6789m;
            var result1 = value1.Copy();
            Assert.Equal(value1, result1);

            var result2 = value1.Map<int>();
            Assert.Equal((int)TypeAnalyzer.Convert(value1, typeof(int)), result2);
        }
        [Fact]
        public void NumberTypesLogger()
        {
            var log = new MapperLog();

            var value1 = 12345.6789m;
            var result1 = value1.Copy(log);
            Assert.Equal(value1, result1);

            var result2 = value1.Map<int>(log);
            Assert.Equal((int)TypeAnalyzer.Convert(value1, typeof(int)), result2);
        }

        [Fact]
        public void EnumTypes()
        {
            var value1 = EnumModel.EnumItem2;
            var result1 = value1.Copy();
            Assert.Equal(value1, result1);
        }
        [Fact]
        public void EnumTypesLogger()
        {
            var log = new MapperLog();

            var value1 = EnumModel.EnumItem2;
            var result1 = value1.Copy(log);
            Assert.Equal(value1, result1);
        }

        [Fact]
        public void ValueTypes()
        {
            var value1 = 5;
            var result1 = value1.Copy();
            Assert.Equal(value1, result1);

            var value2 = new TestStruct { P1 = 1, P2 = 2 };
            var result2 = value2.Copy();
            Assert.Equal(value2.P1, result2.P1);
            Assert.Equal(value2.P2, result2.P2);
        }

        [Fact]
        public void ValueTypesLogger()
        {
            var log = new MapperLog();
            var value1 = 5;
            var result1 = value1.Copy(log);
            Assert.Equal(value1, result1);

            var value2 = new TestStruct { P1 = 1, P2 = 2 };
            var result2 = value2.Copy(log);
            Assert.Equal(value2.P1, result2.P1);
            Assert.Equal(value2.P2, result2.P2);
        }

        [Fact]
        public void ComplexTypes()
        {
            var modelA = ModelA.GetModelA();
            var modelB = modelA.Map<ModelB>();

            ValidateModelAModelB(modelA, modelB);

            modelA = modelB.Map<ModelA>();

            ValidateModelAModelB(modelA, modelB);

            var modelC = modelB.Copy();

            ValidateModelBModelB(modelB, modelC);
        }
        [Fact]
        public void ComplexTypesLogger()
        {
            var log = new MapperLog();
            var modelA = ModelA.GetModelA();
            var modelB = modelA.Map<ModelB>(log);

            ValidateModelAModelB(modelA, modelB);

            log = new MapperLog();
            modelA = modelB.Map<ModelA>(log);

            ValidateModelAModelB(modelA, modelB);

            var modelC = modelB.Copy();

            ValidateModelBModelB(modelB, modelC);
        }

        [Fact]
        public void GetOnlyTypes()
        {
            var model = new ModelGetSetOnly()
            {
                PropA = 5,
                PropB = "Five"
            };
            model.PropDSet = 6;

            var copy = model.Copy();

            Assert.Equal(model.PropA, copy.PropA);
            Assert.Equal(model.PropB, copy.PropB);
            Assert.Equal(model.PropABGet, copy.PropABGet);
            Assert.Equal(model.PropDGet, copy.PropDGet);
        }
        [Fact]
        public void GetOnlyTypesLogger()
        {
            var log = new MapperLog();
            var model = new ModelGetSetOnly()
            {
                PropA = 5,
                PropB = "Five"
            };
            model.PropDSet = 6;

            var copy = model.Copy(log);

            Assert.Equal(model.PropA, copy.PropA);
            Assert.Equal(model.PropB, copy.PropB);
            Assert.Equal(model.PropABGet, copy.PropABGet);
            Assert.Equal(model.PropDGet, copy.PropDGet);
        }

        private static void ValidateModelAModelB(ModelA modelA, ModelB modelB)
        {
            Assert.NotNull(modelB);
            Assert.Equal(modelA.Prop1, modelB.Prop1);
            Assert.Equal(modelA.Prop2, modelB.Prop2);

            Assert.Equal(modelA.ArrayToArray.Length, modelB.ArrayToArray.Length);
            Assert.Equal(modelA.ArrayToArray[0], modelB.ArrayToArray[0]);
            Assert.Equal(modelA.ArrayToArray[1], modelB.ArrayToArray[1]);
            Assert.Equal(modelA.ArrayToArray[2], modelB.ArrayToArray[2]);

            Assert.Equal(modelA.ArrayToList.Length, modelB.ArrayToList.Count);
            Assert.Equal(modelA.ArrayToList[0], modelB.ArrayToList[0]);
            Assert.Equal(modelA.ArrayToList[1], modelB.ArrayToList[1]);
            Assert.Equal(modelA.ArrayToList[2], modelB.ArrayToList[2]);

            Assert.Equal(modelA.ArrayToIList.Length, modelB.ArrayToIList.Count);
            Assert.Equal(modelA.ArrayToIList[0], modelB.ArrayToIList[0]);
            Assert.Equal(modelA.ArrayToIList[1], modelB.ArrayToIList[1]);
            Assert.Equal(modelA.ArrayToIList[2], modelB.ArrayToIList[2]);

            Assert.Equal(modelA.ArrayToSet.Length, modelB.ArrayToSet.Count);
            Assert.True(modelB.ArrayToSet.Contains(modelA.ArrayToSet[0]));
            Assert.True(modelB.ArrayToSet.Contains(modelA.ArrayToSet[1]));
            Assert.True(modelB.ArrayToSet.Contains(modelA.ArrayToSet[2]));

            Assert.Equal(modelA.ArrayToISet.Length, modelB.ArrayToISet.Count);
            Assert.True(modelB.ArrayToISet.Contains(modelA.ArrayToISet[0]));
            Assert.True(modelB.ArrayToISet.Contains(modelA.ArrayToISet[1]));
            Assert.True(modelB.ArrayToISet.Contains(modelA.ArrayToISet[2]));

            Assert.Equal(modelA.ArrayToICollection.Length, modelB.ArrayToICollection.Count);
            Assert.True(modelB.ArrayToICollection.Contains(modelA.ArrayToICollection[0]));
            Assert.True(modelB.ArrayToICollection.Contains(modelA.ArrayToICollection[1]));
            Assert.True(modelB.ArrayToICollection.Contains(modelA.ArrayToICollection[2]));

            Assert.Equal(modelA.ArrayToIEnumerable.Length, modelB.ArrayToIEnumerable.Count());
            Assert.True(modelB.ArrayToIEnumerable.Any(x => x == modelA.ArrayToIEnumerable[0]));
            Assert.True(modelB.ArrayToIEnumerable.Any(x => x == modelA.ArrayToIEnumerable[1]));
            Assert.True(modelB.ArrayToIEnumerable.Any(x => x == modelA.ArrayToIEnumerable[2]));

            Assert.Equal(modelA.ListToArray.Count, modelB.ListToArray.Length);
            Assert.Equal(modelA.ListToArray[0], modelB.ListToArray[0]);
            Assert.Equal(modelA.ListToArray[1], modelB.ListToArray[1]);
            Assert.Equal(modelA.ListToArray[2], modelB.ListToArray[2]);

            Assert.Equal(modelA.ListToList.Count, modelB.ListToList.Count);
            Assert.Equal(modelA.ListToList[0], modelB.ListToList[0]);
            Assert.Equal(modelA.ListToList[1], modelB.ListToList[1]);
            Assert.Equal(modelA.ListToList[2], modelB.ListToList[2]);

            Assert.Equal(modelA.ListToIList.Count, modelB.ListToIList.Count);
            Assert.Equal(modelA.ListToIList[0], modelB.ListToIList[0]);
            Assert.Equal(modelA.ListToIList[1], modelB.ListToIList[1]);
            Assert.Equal(modelA.ListToIList[2], modelB.ListToIList[2]);

            Assert.Equal(modelA.ListToSet.Count, modelB.ListToSet.Count);
            Assert.True(modelB.ListToSet.Contains(modelA.ListToSet[0]));
            Assert.True(modelB.ListToSet.Contains(modelA.ListToSet[1]));
            Assert.True(modelB.ListToSet.Contains(modelA.ListToSet[2]));

            Assert.Equal(modelA.ListToISet.Count, modelB.ListToISet.Count);
            Assert.True(modelB.ListToISet.Contains(modelA.ListToISet[0]));
            Assert.True(modelB.ListToISet.Contains(modelA.ListToISet[1]));
            Assert.True(modelB.ListToISet.Contains(modelA.ListToISet[2]));

            Assert.Equal(modelA.ListToICollection.Count, modelB.ListToICollection.Count);
            Assert.True(modelB.ListToICollection.Contains(modelA.ListToICollection[0]));
            Assert.True(modelB.ListToICollection.Contains(modelA.ListToICollection[1]));
            Assert.True(modelB.ListToICollection.Contains(modelA.ListToICollection[2]));

            Assert.Equal(modelA.ListToIEnumerable.Count, modelB.ListToIEnumerable.Count());
            Assert.True(modelB.ListToIEnumerable.Any(x => x == modelA.ListToIEnumerable[0]));
            Assert.True(modelB.ListToIEnumerable.Any(x => x == modelA.ListToIEnumerable[1]));
            Assert.True(modelB.ListToIEnumerable.Any(x => x == modelA.ListToIEnumerable[2]));

            Assert.Equal(modelA.Dictionary1.Count, modelB.Dictionary1.Count);
            foreach (var item in modelA.Dictionary1)
                Assert.Equal(item.Value, modelB.Dictionary1[item.Key]);

            Assert.Equal(modelA.DictionaryToIDiciontary.Count, modelB.DictionaryToIDiciontary.Count);
            foreach (var item in modelA.DictionaryToIDiciontary)
                Assert.Equal(item.Value, modelB.DictionaryToIDiciontary[item.Key]);
        }
        private static void ValidateModelBModelB(ModelB modelA, ModelB modelB)
        {
            Assert.NotNull(modelB);
            Assert.Equal(modelA.Prop1, modelB.Prop1);
            Assert.Equal(modelA.Prop2, modelB.Prop2);

            Assert.Equal(modelA.ArrayToArray.Length, modelB.ArrayToArray.Length);
            Assert.Equal(modelA.ArrayToArray[0], modelB.ArrayToArray[0]);
            Assert.Equal(modelA.ArrayToArray[1], modelB.ArrayToArray[1]);
            Assert.Equal(modelA.ArrayToArray[2], modelB.ArrayToArray[2]);

            Assert.Equal(modelA.ArrayToList.Count, modelB.ArrayToList.Count);
            Assert.Equal(modelA.ArrayToList[0], modelB.ArrayToList[0]);
            Assert.Equal(modelA.ArrayToList[1], modelB.ArrayToList[1]);
            Assert.Equal(modelA.ArrayToList[2], modelB.ArrayToList[2]);

            Assert.Equal(modelA.ArrayToIList.Count, modelB.ArrayToIList.Count);
            Assert.Equal(modelA.ArrayToIList[0], modelB.ArrayToIList[0]);
            Assert.Equal(modelA.ArrayToIList[1], modelB.ArrayToIList[1]);
            Assert.Equal(modelA.ArrayToIList[2], modelB.ArrayToIList[2]);

            Assert.Equal(modelA.ArrayToSet.Count, modelB.ArrayToSet.Count);
            foreach (var item in modelA.ArrayToSet)
                Assert.True(modelB.ArrayToSet.Contains(item));

            Assert.Equal(modelA.ArrayToISet.Count, modelB.ArrayToISet.Count);
            foreach (var item in modelA.ArrayToISet)
                Assert.True(modelB.ArrayToISet.Contains(item));

            Assert.Equal(modelA.ArrayToICollection.Count, modelB.ArrayToICollection.Count);
            foreach (var item in modelA.ArrayToICollection)
                Assert.True(modelB.ArrayToICollection.Contains(item));

            Assert.Equal(modelA.ArrayToIEnumerable.Count(), modelB.ArrayToIEnumerable.Count());
            foreach (var item in modelA.ArrayToIEnumerable)
                Assert.True(modelB.ArrayToIEnumerable.Contains(item));

            Assert.Equal(modelA.ListToArray.Count(), modelB.ListToArray.Length);
            Assert.Equal(modelA.ListToArray[0], modelB.ListToArray[0]);
            Assert.Equal(modelA.ListToArray[1], modelB.ListToArray[1]);
            Assert.Equal(modelA.ListToArray[2], modelB.ListToArray[2]);

            Assert.Equal(modelA.ListToList.Count, modelB.ListToList.Count);
            Assert.Equal(modelA.ListToList[0], modelB.ListToList[0]);
            Assert.Equal(modelA.ListToList[1], modelB.ListToList[1]);
            Assert.Equal(modelA.ListToList[2], modelB.ListToList[2]);

            Assert.Equal(modelA.ListToIList.Count, modelB.ListToIList.Count);
            Assert.Equal(modelA.ListToIList[0], modelB.ListToIList[0]);
            Assert.Equal(modelA.ListToIList[1], modelB.ListToIList[1]);
            Assert.Equal(modelA.ListToIList[2], modelB.ListToIList[2]);

            Assert.Equal(modelA.ListToSet.Count, modelB.ListToSet.Count);
            foreach (var item in modelA.ListToSet)
                Assert.True(modelB.ListToSet.Contains(item));

            Assert.Equal(modelA.ListToISet.Count, modelB.ListToISet.Count);
            foreach (var item in modelA.ListToISet)
                Assert.True(modelB.ListToISet.Contains(item));

            Assert.Equal(modelA.ListToICollection.Count, modelB.ListToICollection.Count);
            foreach (var item in modelA.ListToICollection)
                Assert.True(modelB.ListToICollection.Contains(item));

            Assert.Equal(modelA.ListToIEnumerable.Count(), modelB.ListToIEnumerable.Count());
            foreach (var item in modelA.ListToIEnumerable)
                Assert.True(modelB.ListToIEnumerable.Contains(item));

            Assert.Equal(modelA.Dictionary1.Count, modelB.Dictionary1.Count);
            foreach (var item in modelA.Dictionary1)
                Assert.Equal(item.Value, modelB.Dictionary1[item.Key]);

            Assert.Equal(modelA.DictionaryToIDiciontary.Count, modelB.DictionaryToIDiciontary.Count);
            foreach (var item in modelA.DictionaryToIDiciontary)
                Assert.Equal(item.Value, modelB.DictionaryToIDiciontary[item.Key]);
        }

        [Fact]
        public void ArrayType()
        {
            var modelA = new ModelA[]
            {
                ModelA.GetModelA(),
                ModelA.GetModelA(),
                ModelA.GetModelA()
            };

            var modelB = modelA.Map<ModelB[]>();
            Assert.NotNull(modelB);
            Assert.Equal(modelA.Length, modelB.Length);
        }
        [Fact]
        public void ArrayTypeLogger()
        {
            var log = new MapperLog();
            var modelA = new ModelA[]
            {
                ModelA.GetModelA(),
                ModelA.GetModelA(),
                ModelA.GetModelA()
            };

            var modelB = modelA.Map<ModelB[]>(log);
            Assert.NotNull(modelB);
            Assert.Equal(modelA.Length, modelB.Length);
        }


        [Fact]
        public void Define()
        {
            var modelA = ModelA.GetModelA();
            var modelB = modelA.Map<ModelA, ModelB>();
            Assert.Equal(int.Parse(modelA.PropA.ToString() + "1"), modelB.PropB);
            Assert.Equal(modelA.PropC, modelB.PropD);

            modelA = modelB.Map<ModelB, ModelA>();
            Assert.Equal(default, modelA.PropA);
            Assert.Equal(modelA.PropC, modelB.PropD);
        }
        [Fact]
        public void DefineLogger()
        {
            var log = new MapperLog();
            var modelA = ModelA.GetModelA();
            var modelB = modelA.Map<ModelA, ModelB>(log);
            Assert.Equal(int.Parse(modelA.PropA.ToString() + "1"), modelB.PropB);
            Assert.Equal(modelA.PropC, modelB.PropD);

            modelA = modelB.Map<ModelB, ModelA>(log);
            Assert.Equal(default, modelA.PropA);
            Assert.Equal(modelA.PropC, modelB.PropD);
        }

        [Fact]
        public void Graph()
        {
            var modelA = ModelA.GetModelA();
            var modelB = modelA.Map<ModelA, ModelB>(new Graph<ModelB>(
                x => x.Prop1,
                x => x.ArrayToList
            ));
            Assert.NotNull(modelB);
            Assert.Equal(modelA.Prop1, modelB.Prop1);
            Assert.Equal(0, modelB.Prop2);
            Assert.NotNull(modelB.ArrayToList);
            Assert.Equal(modelA.ArrayToArray.Length, modelB.ArrayToList.Count);
        }
        [Fact]
        public void GraphLogger()
        {
            var log = new MapperLog();
            var modelA = ModelA.GetModelA();
            var modelB = modelA.Map<ModelA, ModelB>(log, new Graph<ModelB>(
                x => x.Prop1,
                x => x.ArrayToList
            ));
            Assert.NotNull(modelB);
            Assert.Equal(modelA.Prop1, modelB.Prop1);
            Assert.Equal(0, modelB.Prop2);
            Assert.NotNull(modelB.ArrayToList);
            Assert.Equal(modelA.ArrayToArray.Length, modelB.ArrayToList.Count);
        }

        //[Fact]
        //public void Null()
        //{
        //    var modelB = Mapper.Map<ModelB>(null);
        //    Assert.Null(modelB);
        //}
        //[Fact]
        //public void NullLogger()
        //{
        //    var log = new MapperLog();
        //    var modelB = MapperWithLog.Map<ModelB>(null, log);
        //    Assert.Null(modelB);
        //}

        [Fact]
        public void Collections()
        {
            var modelAs = new ModelA[] { ModelA.GetModelA(), ModelA.GetModelA(), ModelA.GetModelA() };

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

            Assert.Equal(typeof(ModelB[]), modelBs.GetType());
            Assert.Equal(typeof(List<ModelB>), modelCs.GetType());
            Assert.Equal(typeof(ModelB[]), modelDs.GetType());
            Assert.Equal(typeof(ModelB[]), modelEs.GetType());

            Assert.Equal(typeof(List<ModelB>), modelFs.GetType());
            Assert.Equal(typeof(HashSet<ModelB>), modelGs.GetType());
            Assert.Equal(typeof(ModelB[]), modelHs.GetType());
            Assert.Equal(typeof(ModelB[]), modelIs.GetType());

            Assert.Equal(typeof(List<ModelB>), modelJs.GetType());
            Assert.Equal(typeof(HashSet<ModelB>), modelKs.GetType());
            Assert.Equal(typeof(List<ModelB>), modelLs.GetType());
            Assert.Equal(typeof(ModelB[]), modelMs.GetType());
        }
        [Fact]
        public void CollectionsLogger()
        {
            var log = new MapperLog();
            var modelAs = new ModelA[] { ModelA.GetModelA(), ModelA.GetModelA(), ModelA.GetModelA() };

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

            Assert.Equal(typeof(ModelB[]), modelBs.GetType());
            Assert.Equal(typeof(List<ModelB>), modelCs.GetType());
            Assert.Equal(typeof(ModelB[]), modelDs.GetType());
            Assert.Equal(typeof(ModelB[]), modelEs.GetType());

            Assert.Equal(typeof(List<ModelB>), modelFs.GetType());
            Assert.Equal(typeof(HashSet<ModelB>), modelGs.GetType());
            Assert.Equal(typeof(ModelB[]), modelHs.GetType());
            Assert.Equal(typeof(ModelB[]), modelIs.GetType());

            Assert.Equal(typeof(List<ModelB>), modelJs.GetType());
            Assert.Equal(typeof(HashSet<ModelB>), modelKs.GetType());
            Assert.Equal(typeof(List<ModelB>), modelLs.GetType());
            Assert.Equal(typeof(ModelB[]), modelMs.GetType());
        }

        [Fact]
        public void Recursion()
        {
            var modelA = new TA() { ID = 1 };
            modelA.Prop1 = modelA;
            modelA.Prop2 = modelA;

            var result = modelA.Map<TB>();
        }
        [Fact]
        public void RecursionLogger()
        {
            var log = new MapperLog();
            var modelA = new TA() { ID = 1 };
            modelA.Prop1 = modelA;
            modelA.Prop2 = modelA;

            var result = modelA.Map<TB>(log);
        }

        [Fact]
        public void CastedType()
        {
            var modelA = new ModelA[]
            {
                ModelA.GetModelA(),
                ModelA.GetModelA(),
                ModelA.GetModelA()
            };

            var modelB = modelA.Map<ICollection<ModelB>>();
            Assert.NotNull(modelB);
            Assert.Equal(modelA.Length, modelB.Count);
        }
        [Fact]
        public void CastedTypeLogger()
        {
            var log = new MapperLog();
            var modelA = new ModelA[]
            {
                ModelA.GetModelA(),
                ModelA.GetModelA(),
                ModelA.GetModelA()
            };

            var modelB = modelA.Map<ICollection<ModelB>>(log);
            Assert.NotNull(modelB);
            Assert.Equal(modelA.Length, modelB.Count);
        }

        [Fact]
        public void StringConversions()
        {
            var model = TypesCoreModel.Create();

            var modelStrings = model.Map<TypesCoreAsStringsModel>();
            TypesCoreModel.AssertAreEqual(model, modelStrings);

            model = modelStrings.Map<TypesCoreModel>();
            TypesCoreModel.AssertAreEqual(model, modelStrings);
        }
    }
}
