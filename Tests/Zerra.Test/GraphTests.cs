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
        public void ConstructorMembers()
        {
            var graph = new Graph<GraphModel>(
                x => x.Prop1,
                x => x.Array.Select(x => x.Value1),
                x => x.Class.Value1
            );

            TestBasic(graph);
        }

        [TestMethod]
        public void ConstructorAllMembers()
        {
            var graph = new Graph<GraphModel>(true);

            foreach (var member in TypeAnalyzer<GraphModel>.GetTypeDetail().MemberDetails)
            {
                Assert.IsTrue(graph.HasMember(member.Name));
            }

            graph.AddChildGraph(x => x.Array, new Graph<SimpleModel>(x => x.Value1));
            Assert.IsTrue(graph.HasMember(x => x.Array));
        }

        [TestMethod]
        public void AddMembers()
        {
            var graph = new Graph<GraphModel>();

            graph.AddMembers(
                x => x.Prop1,
                x => x.Array.Select(x => x.Value1),
                x => x.Class.Value1
            );

            TestBasic(graph);

            var childGraph1 = graph.GetChildGraph<SimpleModel>(x => x.Array);
            Assert.IsNotNull(childGraph1);
            Assert.IsTrue(childGraph1.HasMember(x => x.Value1));
            Assert.IsFalse(childGraph1.HasMember(x => x.Value2));

            graph.AddMember(x => x.Array);

            childGraph1 = graph.GetChildGraph<SimpleModel>(x => x.Array);
            Assert.IsNotNull(childGraph1);
            Assert.IsTrue(childGraph1.HasMember(x => x.Value1));
            Assert.IsTrue(childGraph1.HasMember(x => x.Value2));
        }

        [TestMethod]
        public void RemoveMembers()
        {
            var graph = new Graph<GraphModel>(
                x => x.Prop1,
                x => x.Prop2,
                x => x.Array.Select(x => x.Value1),
                x => x.List.Select(x => x.Value1),
                x => x.Class.Value1
            );

            graph.RemoveMembers(
                x => x.Prop2,
                x => x.List
            );

            TestBasic(graph);
        }

        [TestMethod]
        public void RemoveChildMembers()
        {
            var graph = new Graph<GraphModel>(true);

            graph.RemoveMember(x => x.Class.Value1);

            var childGraph1 = graph.GetChildGraph<SimpleModel>(x => x.Class);
            Assert.IsNotNull(childGraph1);
            Assert.IsNotNull(childGraph1);
        }

        [TestMethod]
        public void AccessRemovedChildMember()
        {
            var graph = new Graph<GraphModel>(
                x => x.Prop1,
                x => x.Array.Select(x => x.Value1),
                x => x.Class.Value1
            );

            graph.RemoveMembers(x => x.List);
            graph.AddMember(x => x.List.Select(y => y.Value2)); //should be ignored

            TestBasic(graph);
        }

        [TestMethod]
        public void MultipleStackExpression()
        {
            var graph = new Graph<GraphModel>(
                x => x.Nested.NestedArray.Select(y => y.Class.Value1)    
            );

            var childGraph1 = graph.GetChildGraph<GraphModel>(x => x.Nested);
            var childGraph2 = childGraph1.GetChildGraph<GraphModel>(x => x.NestedArray);
            var childGraph3 = childGraph2.GetChildGraph<SimpleModel>(x => x.Class);
            childGraph2.HasMember(nameof(SimpleModel.Value1));
        }

        [TestMethod]
        public void AddChildGraph()
        {
            var graph = new Graph<GraphModel>(
                x => x.Prop1
            );

            var childGraph = new Graph<SimpleModel>(x => x.Value1);
            graph.AddChildGraph(x => x.Array, childGraph);

            var childGraph2 = new Graph<SimpleModel>(x => x.Value1);
            graph.AddChildGraph(x => x.Class, childGraph2);

            TestBasic(graph);
        }

        [TestMethod]
        public void AddInstanceGraph()
        {
            var graph = new Graph<GraphModel>(new Graph<GraphModel>(
                x => x.Prop1,
                x => x.Array.Select(x => x.Value1),
                x => x.Class.Value1
            ));

            var instance = GraphModel.Create();
            graph.AddInstanceGraph(instance, new Graph<GraphModel>(
                x => x.Array.Select(x => x.Value1)
            ));
            var instanceGraph = graph.GetInstanceGraph(instance);

            TestBasic(graph);
            Assert.IsFalse(instanceGraph.HasMember(nameof(GraphModel.Prop1)));
            Assert.IsTrue(instanceGraph.HasMember(nameof(GraphModel.Array)));

            var instance2 = GraphModel.Create();
            var instanceGraph2 = graph.GetInstanceGraph(instance2);
            TestBasic(instanceGraph2);

            var childGraph = graph.GetChildGraph(nameof(GraphModel.Array));
            var childInstance = new SimpleModel() { Value1 = 1, Value2 = "2" };
            childGraph.AddInstanceGraph(childInstance, new Graph<SimpleModel>(x => x.Value2));

            var childInstanceGraph = graph.GetChildInstanceGraph(nameof(GraphModel.Array), childInstance);
            Assert.IsFalse(childInstanceGraph.HasMember(nameof(SimpleModel.Value1)));
            Assert.IsTrue(childInstanceGraph.HasMember(nameof(SimpleModel.Value2)));
        }

        [TestMethod]
        public void Copy()
        {
            var graph = new Graph<GraphModel>(
                x => x.Prop1,
                x => x.Array.Select(x => x.Value1),
                x => x.Class.Value1
            );

            var copy = new Graph<GraphModel>(graph);

            TestBasic(copy);
        }

        [TestMethod]
        public void Convert()
        {
            var graph = new Graph(nameof(GraphModel.Prop1));
            graph.AddChildGraph(nameof(GraphModel.Array), new Graph(nameof(SimpleModel.Value1)));
            graph.AddChildGraph(nameof(GraphModel.Class), new Graph(nameof(SimpleModel.Value1)));

            TestBasic(graph);

            var converted = new Graph<GraphModel>(graph);

            TestBasic(converted);
        }

        [TestMethod]
        public void Compare()
        {
            var graph1 = new Graph<GraphModel>(
                x => x.Prop1,
                x => x.Array.Select(x => x.Value1)
            );

            var graph2 = new Graph<GraphModel>(
                x => x.Prop1,
                x => x.Array.Select(x => x.Value1)
            );

            var graph3 = new Graph<GraphModel>(
                x => x.Prop1
            );

            Assert.IsTrue(graph1.Equals(graph2));
            Assert.IsFalse(graph1.Equals(graph3));

            Assert.IsTrue(graph1 == graph2);
            Assert.IsFalse(graph1 == graph3);
            Assert.IsTrue(graph1 != graph3);
        }

        //[TestMethod]
        //public void Select()
        //{
        //    var graph = new Graph<GraphModel>(
        //        x => x.BooleanThing,
        //        x => x.ClassThing.Value1,
        //        x => x.ClassArray.Select(x => x.Value2)
        //    );

        //    var select = graph.GenerateSelect<GraphModel>();
        //    var model = GraphModel.Create();
        //    var result = select.Compile().Invoke(model);

        //    Assert.AreEqual(model.BooleanThing, result.BooleanThing);
        //    Assert.AreNotEqual(model.ByteThing, result.ByteThing);

        //    Assert.IsNotNull(result.ClassThing);
        //    Assert.AreEqual(model.ClassThing.Value1, result.ClassThing.Value1);
        //    Assert.AreNotEqual(model.ClassThing.Value2, result.ClassThing.Value2);

        //    Assert.IsNotNull(result.ClassArray);
        //    Assert.AreEqual(model.ClassArray.Length, result.ClassArray.Length);
        //    Assert.AreNotEqual(model.ClassArray[0].Value1, result.ClassArray[0].Value1);
        //    Assert.AreEqual(model.ClassArray[0].Value2, result.ClassArray[0].Value2);
        //}

        [TestMethod]
        public void ToStringer()
        {
            var graph = new Graph<GraphModel>(
                x => x.Prop1,
                x => x.Array.Select(x => x.Value1),
                x => x.Class.Value1
            );

            var str = graph.ToString();
            const string strCheck = @"Prop1
Array
  Value1
Class
  Value1";
            Assert.AreEqual(strCheck, str);
        }

        private void TestBasic(Graph graph)
        {
            //Validates graph only has these
            //x => x.Prop1,
            //x => x.Array.Select(x => x.Value1)
            //x => x.Class.Select(x => x.Value1)

            Assert.IsTrue(graph.HasMember(nameof(GraphModel.Prop1)));
            Assert.IsFalse(graph.HasMember(nameof(GraphModel.Prop2)));

            Assert.IsTrue(graph.HasMember(nameof(GraphModel.Array)));
            Assert.IsFalse(graph.HasMember(nameof(GraphModel.List)));

            var childGraph1 = graph.GetChildGraph<SimpleModel>(nameof(GraphModel.Array));
            Assert.IsNotNull(childGraph1);

            Assert.IsTrue(childGraph1.HasMember(nameof(SimpleModel.Value1)));
            Assert.IsFalse(childGraph1.HasMember(nameof(SimpleModel.Value2)));

            var childGraph2 = graph.GetChildGraph<SimpleModel>(nameof(GraphModel.List));
            Assert.IsNull(childGraph2);

            var childGraph3 = graph.GetChildGraph<SimpleModel>(nameof(GraphModel.Class));
            Assert.IsNotNull(childGraph3);
            Assert.IsTrue(childGraph3.HasMember(nameof(SimpleModel.Value1)));
        }
    }
}
