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
                Assert.IsTrue(graph.HasLocalProperty(member.Name));
                Assert.IsTrue(graph.HasProperty(member.Name));
                Assert.IsTrue(graph.LocalProperties.Contains(member.Name));
            }

            graph.AddChildGraph(new Graph<SimpleModel>(nameof(TypesAllModel.ClassArray), x => x.Value1));
            Assert.IsFalse(graph.HasLocalProperty(nameof(TypesAllModel.ClassArray)));
            Assert.IsTrue(graph.HasProperty(nameof(TypesAllModel.ClassArray)));
            Assert.IsFalse(graph.LocalProperties.Contains(nameof(TypesAllModel.ClassArray)));
        }

        [TestMethod]
        public void AddProperties()
        {
            var graph = new Graph<TypesAllModel>();

            graph.AddProperties(
                x => x.BooleanThing,
                x => x.ClassArray.Select(x => x.Value1)
            );

            TestBasic(graph);
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

            graph.RemoveProperties(
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
            var childGraph = new Graph<SimpleModel>(nameof(TypesAllModel.ClassArray), x => x.Value1);

            graph.AddChildGraph(childGraph);

            TestBasic(graph);

            var childGraph2 = new Graph<SimpleModel>(nameof(TypesAllModel.ClassArray), x => x.Value2);
            graph.AddChildGraph(childGraph2);

            Assert.IsTrue(childGraph.HasProperty(nameof(SimpleModel.Value1)));
            Assert.IsTrue(childGraph.HasProperty(nameof(SimpleModel.Value2)));
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
            Assert.IsFalse(instanceGraph.HasProperty(nameof(TypesAllModel.BooleanThing)));
            Assert.IsTrue(instanceGraph.HasProperty(nameof(TypesAllModel.ClassArray)));

            var instance2 = TypesAllModel.Create();
            var instanceGraph2 = graph.GetInstanceGraph(instance2);
            TestBasic(instanceGraph2);

            var childGraph = graph.GetChildGraph(nameof(TypesAllModel.ClassArray));
            var childInstance = new SimpleModel() { Value1 = 1, Value2 = "2" };
            childGraph.AddInstanceGraph(childInstance, new Graph<SimpleModel>(x => x.Value2));

            var childInstanceGraph = graph.GetChildInstanceGraph(nameof(TypesAllModel.ClassArray), childInstance);
            Assert.IsFalse(childInstanceGraph.HasProperty(nameof(SimpleModel.Value1)));
            Assert.IsTrue(childInstanceGraph.HasProperty(nameof(SimpleModel.Value2)));
        }

        [TestMethod]
        public void Copy()
        {
            var graph = new Graph<TypesAllModel>(
                x => x.BooleanThing,
                x => x.ClassArray.Select(x => x.Value1)
            );

            var copy = graph.Copy();

            TestBasic(copy);
        }

        [TestMethod]
        public void Convert()
        {
            var graph = new Graph(
                [nameof(TypesAllModel.BooleanThing)],
                [new Graph(nameof(TypesAllModel.ClassArray), false, nameof(SimpleModel.Value1))]
            );

            TestBasic(graph);

            var converted = graph.Convert<TypesAllModel>();

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

            Assert.IsTrue(graph.HasProperty(nameof(TypesAllModel.BooleanThing)));
            Assert.IsTrue(graph.HasLocalProperty(nameof(TypesAllModel.BooleanThing)));
            Assert.IsFalse(graph.HasProperty(nameof(TypesAllModel.ByteThing)));
            Assert.IsFalse(graph.HasLocalProperty(nameof(TypesAllModel.ByteThing)));

            Assert.IsTrue(graph.HasProperty(nameof(TypesAllModel.ClassArray)));
            Assert.IsFalse(graph.HasLocalProperty(nameof(TypesAllModel.ClassArray)));
            Assert.IsFalse(graph.HasProperty(nameof(TypesAllModel.ClassList)));
            Assert.IsFalse(graph.HasLocalProperty(nameof(TypesAllModel.ClassList)));

            Assert.IsTrue(graph.HasChild(nameof(TypesAllModel.ClassArray)));
            Assert.IsFalse(graph.HasChild(nameof(TypesAllModel.ClassList)));

            var childGraph = graph.GetChildGraph<SimpleModel>(nameof(TypesAllModel.ClassArray));
            Assert.IsNotNull(childGraph);

            Assert.IsTrue(childGraph.HasProperty(nameof(SimpleModel.Value1)));
            Assert.IsFalse(childGraph.HasProperty(nameof(SimpleModel.Value2)));

            Assert.IsTrue(graph.LocalProperties.Count() == 1);
            Assert.IsTrue(graph.LocalProperties.First() == nameof(TypesAllModel.BooleanThing));
        }
    }
}
