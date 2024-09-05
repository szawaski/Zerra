// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using Zerra.Reflection;

namespace Zerra.Test
{
    [TestClass]
    public class GraphTests
    {
        [TestMethod]
        public void ConstructorProperties()
        {
            var graph = new Graph<TypesAllModel>(
                x => x.BooleanThing,
                x => x.ClassArray.Select(x => x.Value1)
            );

            TestBasic(graph);
        }

        [TestMethod]
        public void ConstructorAllProperties()
        {
            var graph = new Graph<TypesAllModel>(true);

            foreach (var member in TypeAnalyzer<TypesAllModel>.GetTypeDetail().MemberDetails)
            {
                Assert.IsTrue(graph.HasMember(member.Name));
            }

            graph.AddOrMergeChildGraph(nameof(TypesAllModel.ClassArray), new Graph<SimpleModel>(x => x.Value1));
            Assert.IsTrue(graph.HasMember(nameof(TypesAllModel.ClassArray)));
        }

        [TestMethod]
        public void AddProperties()
        {
            var graph = new Graph<TypesAllModel>();

            graph.AddMembers(
                x => x.BooleanThing,
                x => x.ClassArray.Select(x => x.Value1)
            );

            TestBasic(graph);

            graph.AddMember(x => x.ClassArray);

            var childGraph1 = graph.GetChildGraph<SimpleModel>(nameof(TypesAllModel.ClassArray));
            Assert.IsNotNull(childGraph1);
            Assert.IsTrue(childGraph1.HasMember(nameof(SimpleModel.Value2)));
        }

        [TestMethod]
        public void RemoveProperties()
        {
            var graph = new Graph<TypesAllModel>(
                x => x.BooleanThing,
                x => x.ByteThing,
                x => x.ClassArray.Select(x => x.Value1),
                x => x.ClassList.Select(x => x.Value1)
            );

            graph.RemoveMembers(
                x => x.ByteThing,
                x => x.ClassList
            );

            TestBasic(graph);
        }

        [TestMethod]
        public void AddChildGraph()
        {
            var graph = new Graph<TypesAllModel>(
                x => x.BooleanThing
            );
            var childGraph = new Graph<SimpleModel>(x => x.Value1);
            graph.AddOrMergeChildGraph(nameof(TypesAllModel.ClassArray), childGraph);

            TestBasic(graph);

            var childGraph2 = new Graph<SimpleModel>(x => x.Value2);
            graph.AddOrMergeChildGraph(nameof(TypesAllModel.ClassArray), childGraph2);

            Assert.IsTrue(childGraph.HasMember(nameof(SimpleModel.Value1)));
            Assert.IsTrue(childGraph.HasMember(nameof(SimpleModel.Value2)));
        }

        [TestMethod]
        public void AddInstanceGraph()
        {
            var graph = new Graph<TypesAllModel>(new Graph<TypesAllModel>(
                x => x.BooleanThing,
                x => x.ClassArray.Select(x => x.Value1)
            ));

            var instance = TypesAllModel.Create();
            graph.AddInstanceGraph(instance, new Graph<TypesAllModel>(
                x => x.ClassArray.Select(x => x.Value1)
            ));
            var instanceGraph = graph.GetInstanceGraph(instance);

            TestBasic(graph);
            Assert.IsFalse(instanceGraph.HasMember(nameof(TypesAllModel.BooleanThing)));
            Assert.IsTrue(instanceGraph.HasMember(nameof(TypesAllModel.ClassArray)));

            var instance2 = TypesAllModel.Create();
            var instanceGraph2 = graph.GetInstanceGraph(instance2);
            TestBasic(instanceGraph2);

            var childGraph = graph.GetChildGraph(nameof(TypesAllModel.ClassArray));
            var childInstance = new SimpleModel() { Value1 = 1, Value2 = "2" };
            childGraph.AddInstanceGraph(childInstance, new Graph<SimpleModel>(x => x.Value2));

            var childInstanceGraph = graph.GetChildInstanceGraph(nameof(TypesAllModel.ClassArray), childInstance);
            Assert.IsFalse(childInstanceGraph.HasMember(nameof(SimpleModel.Value1)));
            Assert.IsTrue(childInstanceGraph.HasMember(nameof(SimpleModel.Value2)));
        }

        [TestMethod]
        public void Copy()
        {
            var graph = new Graph<TypesAllModel>(
                x => x.BooleanThing,
                x => x.ClassArray.Select(x => x.Value1)
            );

            var copy = new Graph<TypesAllModel>(graph);

            TestBasic(copy);
        }

        [TestMethod]
        public void Convert()
        {
            var graph = new Graph(nameof(TypesAllModel.BooleanThing));
            graph.AddOrMergeChildGraph(nameof(TypesAllModel.ClassArray), new Graph(false, nameof(SimpleModel.Value1)));

            TestBasic(graph);

            var converted = new Graph<TypesAllModel>(graph);

            TestBasic(converted);
        }

        [TestMethod]
        public void Compare()
        {
            var graph1 = new Graph<TypesAllModel>(
                x => x.BooleanThing,
                x => x.ClassArray.Select(x => x.Value1)
            );

            var graph2 = new Graph<TypesAllModel>(
                x => x.BooleanThing,
                x => x.ClassArray.Select(x => x.Value1)
            );

            var graph3 = new Graph<TypesAllModel>(
                x => x.BooleanThing
            );

            Assert.IsTrue(graph1.Equals(graph2));
            Assert.IsFalse(graph1.Equals(graph3));

            Assert.IsTrue(graph1 == graph2);
            Assert.IsFalse(graph1 == graph3);
            Assert.IsTrue(graph1 != graph3);
        }

        [TestMethod]
        public void Select()
        {
            var graph = new Graph<TypesAllModel>(
                x => x.BooleanThing,
                x => x.ClassThing.Value1,
                x => x.ClassArray.Select(x => x.Value2)
            );

            var select = graph.GenerateSelect<TypesAllModel>();
            var model = TypesAllModel.Create();
            var result = select.Compile().Invoke(model);

            Assert.AreEqual(model.BooleanThing, result.BooleanThing);
            Assert.AreNotEqual(model.ByteThing, result.ByteThing);

            Assert.IsNotNull(result.ClassThing);
            Assert.AreEqual(model.ClassThing.Value1, result.ClassThing.Value1);
            Assert.AreNotEqual(model.ClassThing.Value2, result.ClassThing.Value2);

            Assert.IsNotNull(result.ClassArray);
            Assert.AreEqual(model.ClassArray.Length, result.ClassArray.Length);
            Assert.AreNotEqual(model.ClassArray[0].Value1, result.ClassArray[0].Value1);
            Assert.AreEqual(model.ClassArray[0].Value2, result.ClassArray[0].Value2);
        }

        [TestMethod]
        public void ToStringer()
        {
            var graph = new Graph<TypesAllModel>(
                x => x.BooleanThing,
                x => x.ClassArray.Select(x => x.Value1)
            );

            var str = graph.ToString();
            const string strCheck = @"BooleanThing
ClassArray
  Value1";
            Assert.AreEqual(strCheck, str);
        }

        private void TestBasic(Graph graph)
        {
            //Validates graph has these but not ByteThing or ClassList
            //x => x.BooleanThing,
            //x => x.ClassArray.Select(x => x.Value1)

            Assert.IsTrue(graph.HasMember(nameof(TypesAllModel.BooleanThing)));
            Assert.IsFalse(graph.HasMember(nameof(TypesAllModel.ByteThing)));

            Assert.IsTrue(graph.HasMember(nameof(TypesAllModel.ClassArray)));
            Assert.IsFalse(graph.HasMember(nameof(TypesAllModel.ClassList)));

            var childGraph1 = graph.GetChildGraph<SimpleModel>(nameof(TypesAllModel.ClassArray));
            Assert.IsNotNull(childGraph1);

            Assert.IsTrue(childGraph1.HasMember(nameof(SimpleModel.Value1)));
            Assert.IsFalse(childGraph1.HasMember(nameof(SimpleModel.Value2)));

            var childGraph2 = graph.GetChildGraph<SimpleModel>(nameof(TypesAllModel.ClassList));
            Assert.IsNull(childGraph2);

        }
    }
}
