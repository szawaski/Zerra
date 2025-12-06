// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using Zerra.Map;
using Zerra.SourceGeneration;
using Zerra.Test.Helpers.Models;

namespace Zerra.Test.Map
{
    public class MapTests
    {
        static MapTests()
        {
            MapCustomizations.Register(new DefineModelAToModelB());
        }

        [Fact]
        public void StringTypes()
        {
            var value1 = "Test";
            var result1 = value1.Copy();
            Assert.Equal(value1, result1);
        }

        [Fact]
        public void NumberTypes()
        {
            var value1 = 12345.6789m;
            var result1 = value1.Copy();
            Assert.Equal(value1, result1);

            var result2 = value1.Map<decimal, int>();
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
        public void ComplexTypes()
        {
            var modelA = ModelA.GetModelA();
            var modelB = modelA.Map<ModelA, ModelB>();

            ValidateModelAModelB(modelA, modelB);

            modelA = modelB.Map<ModelB, ModelA>();

            ValidateModelAModelB(modelA, modelB);

            var modelC = modelB.Copy();

            ValidateModelBModelB(modelB, modelC);
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
            Assert.Contains(modelA.ArrayToSet[0], modelB.ArrayToSet);
            Assert.Contains(modelA.ArrayToSet[1], modelB.ArrayToSet);
            Assert.Contains(modelA.ArrayToSet[2], modelB.ArrayToSet);

            Assert.Equal(modelA.ArrayToISet.Length, modelB.ArrayToISet.Count);
            Assert.True(modelB.ArrayToISet.Contains(modelA.ArrayToISet[0]));
            Assert.True(modelB.ArrayToISet.Contains(modelA.ArrayToISet[1]));
            Assert.True(modelB.ArrayToISet.Contains(modelA.ArrayToISet[2]));

            Assert.Equal(modelA.ArrayToICollection.Length, modelB.ArrayToICollection.Count);
            Assert.True(modelB.ArrayToICollection.Contains(modelA.ArrayToICollection[0]));
            Assert.True(modelB.ArrayToICollection.Contains(modelA.ArrayToICollection[1]));
            Assert.True(modelB.ArrayToICollection.Contains(modelA.ArrayToICollection[2]));

            Assert.Equal(modelA.ArrayToIEnumerable.Length, modelB.ArrayToIEnumerable.Count());
            Assert.Contains(modelB.ArrayToIEnumerable, x => x == modelA.ArrayToIEnumerable[0]);
            Assert.Contains(modelB.ArrayToIEnumerable, x => x == modelA.ArrayToIEnumerable[1]);
            Assert.Contains(modelB.ArrayToIEnumerable, x => x == modelA.ArrayToIEnumerable[2]);

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
            Assert.Contains(modelA.ListToSet[0], modelB.ListToSet);
            Assert.Contains(modelA.ListToSet[1], modelB.ListToSet);
            Assert.Contains(modelA.ListToSet[2], modelB.ListToSet);

            Assert.Equal(modelA.ListToISet.Count, modelB.ListToISet.Count);
            Assert.True(modelB.ListToISet.Contains(modelA.ListToISet[0]));
            Assert.True(modelB.ListToISet.Contains(modelA.ListToISet[1]));
            Assert.True(modelB.ListToISet.Contains(modelA.ListToISet[2]));

            Assert.Equal(modelA.ListToICollection.Count, modelB.ListToICollection.Count);
            Assert.True(modelB.ListToICollection.Contains(modelA.ListToICollection[0]));
            Assert.True(modelB.ListToICollection.Contains(modelA.ListToICollection[1]));
            Assert.True(modelB.ListToICollection.Contains(modelA.ListToICollection[2]));

            Assert.Equal(modelA.ListToIEnumerable.Count, modelB.ListToIEnumerable.Count());
            Assert.Contains(modelB.ListToIEnumerable, x => x == modelA.ListToIEnumerable[0]);
            Assert.Contains(modelB.ListToIEnumerable, x => x == modelA.ListToIEnumerable[1]);
            Assert.Contains(modelB.ListToIEnumerable, x => x == modelA.ListToIEnumerable[2]);

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
                Assert.Contains(item, modelB.ArrayToSet);

            Assert.Equal(modelA.ArrayToISet.Count, modelB.ArrayToISet.Count);
            foreach (var item in modelA.ArrayToISet)
                Assert.True(modelB.ArrayToISet.Contains(item));

            Assert.Equal(modelA.ArrayToICollection.Count, modelB.ArrayToICollection.Count);
            foreach (var item in modelA.ArrayToICollection)
                Assert.True(modelB.ArrayToICollection.Contains(item));

            Assert.Equal(modelA.ArrayToIEnumerable.Count(), modelB.ArrayToIEnumerable.Count());
            foreach (var item in modelA.ArrayToIEnumerable)
                Assert.Contains(item, modelB.ArrayToIEnumerable);

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
                Assert.Contains(item, modelB.ListToSet);

            Assert.Equal(modelA.ListToISet.Count, modelB.ListToISet.Count);
            foreach (var item in modelA.ListToISet)
                Assert.True(modelB.ListToISet.Contains(item));

            Assert.Equal(modelA.ListToICollection.Count, modelB.ListToICollection.Count);
            foreach (var item in modelA.ListToICollection)
                Assert.True(modelB.ListToICollection.Contains(item));

            Assert.Equal(modelA.ListToIEnumerable.Count(), modelB.ListToIEnumerable.Count());
            foreach (var item in modelA.ListToIEnumerable)
                Assert.Contains(item, modelB.ListToIEnumerable);

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

            var modelB = modelA.Map<ModelA[], ModelB[]>();
            Assert.NotNull(modelB);
            Assert.Equal(modelA.Length, modelB.Length);
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
        public void Collections()
        {
            var modelAs = new ModelA[] { ModelA.GetModelA(), ModelA.GetModelA(), ModelA.GetModelA() };

            var modelBs = modelAs.Map<ModelA[], ICollection<ModelB>>();
            var modelCs = modelAs.Map<ModelA[], IReadOnlyCollection<ModelB>>();
            var modelDs = modelAs.Map<ModelA[], IList<ModelB>>();
            var modelEs = modelAs.Map<ModelA[], IReadOnlyList<ModelB>>();
            var modelFs = modelAs.Map<ModelA[], ISet<ModelB>>();
            var modelGs = modelAs.Map<ModelA[], IReadOnlySet<ModelB>>();
            var modelHs = modelAs.Map<ModelA[], IEnumerable<ModelB>>();
            var modelIs = modelAs.Map<ModelA[], ModelB[]>();

            Assert.Equal(typeof(List<ModelB>), modelBs.GetType());
            Assert.Equal(typeof(List<ModelB>), modelCs.GetType());
            Assert.Equal(typeof(List<ModelB>), modelDs.GetType());
            Assert.Equal(typeof(List<ModelB>), modelEs.GetType());
            Assert.Equal(typeof(HashSet<ModelB>), modelFs.GetType());
            Assert.Equal(typeof(HashSet<ModelB>), modelGs.GetType());
            Assert.Equal(typeof(ModelB[]), modelHs.GetType());
            Assert.Equal(typeof(ModelB[]), modelIs.GetType());
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

            var modelB = modelA.Map<ModelA[], ICollection<ModelB>>();
            Assert.NotNull(modelB);
            Assert.Equal(modelA.Length, modelB.Count);
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
    }
}
