// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using System.Reflection;
using Zerra.Test.Helpers.Models;

namespace Zerra.Test
{
    public class GraphTests
    {
        [Fact]
        public void ConstructorMembers()
        {
            var graph = new Graph<GraphModel>(
                x => x.Prop1,
                x => x.Array.Select(x => x.Value1),
                x => x.Class.Value1
            );

            TestBasic(graph);
        }

        [Fact]
        public void ConstructorAllMembers()
        {
            var graph = new Graph<GraphModel>(true);

            foreach (var member in typeof(GraphModel).GetMembers(BindingFlags.Public | BindingFlags.Instance))
            {
                Assert.True(graph.HasMember(member.Name));
            }

            graph.AddChildGraph(x => x.Array, new Graph<SimpleModel>(x => x.Value1));
            Assert.True(graph.HasMember(x => x.Array));
        }

        [Fact]
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
            Assert.NotNull(childGraph1);
            Assert.True(childGraph1.HasMember(x => x.Value1));
            Assert.False(childGraph1.HasMember(x => x.Value2));

            graph.AddMember(x => x.Array);

            childGraph1 = graph.GetChildGraph<SimpleModel>(x => x.Array);
            Assert.NotNull(childGraph1);
            Assert.True(childGraph1.HasMember(x => x.Value1));
            Assert.True(childGraph1.HasMember(x => x.Value2));
        }

        [Fact]
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

        [Fact]
        public void AddChildMembers()
        {
            var graph = new Graph<GraphModel>(x => x.Class);
            graph.AddMember(x => x.Class.Value2);

            var childGraph1 = graph.GetChildGraph<SimpleModel>(x => x.Class);
            Assert.NotNull(childGraph1);
            Assert.True(childGraph1.HasMember(x => x.Value1));
            Assert.True(childGraph1.HasMember(x => x.Value2));
            Assert.True(childGraph1.IncludeAllMembers);

            graph = new Graph<GraphModel>();
            graph.AddMember(x => x.Class.Value2);

            childGraph1 = graph.GetChildGraph<SimpleModel>(x => x.Class);
            Assert.NotNull(childGraph1);
            Assert.False(childGraph1.HasMember(x => x.Value1));
            Assert.True(childGraph1.HasMember(x => x.Value2));
            Assert.False(childGraph1.IncludeAllMembers);
        }

        [Fact]
        public void RemoveChildMembers()
        {
            var graph = new Graph<GraphModel>(true);

            graph.RemoveMember(x => x.Class.Value1);

            var childGraph1 = graph.GetChildGraph<SimpleModel>(x => x.Class);
            Assert.NotNull(childGraph1);
            Assert.False(childGraph1.HasMember(x => x.Value1));
            Assert.True(childGraph1.HasMember(x => x.Value2));
            Assert.True(childGraph1.IncludeAllMembers);

            graph = new Graph<GraphModel>();

            graph.RemoveMember(x => x.Class.Value1);

            childGraph1 = graph.GetChildGraph<SimpleModel>(x => x.Class);
            Assert.NotNull(childGraph1);
            Assert.False(childGraph1.HasMember(x => x.Value1));
            Assert.False(childGraph1.HasMember(x => x.Value2));
            Assert.False(childGraph1.IncludeAllMembers);
        }

        [Fact]
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

        [Fact]
        public void MultipleStackExpression()
        {
            var graph = new Graph<GraphModel>(
                x => x.Nested.NestedArray.Select(y => y.Class.Value1)    
            );

            var childGraph1 = graph.GetChildGraph<GraphModel>(x => x.Nested);
            var childGraph2 = childGraph1.GetChildGraph<GraphModel>(x => x.NestedArray);
            var childGraph3 = childGraph2.GetChildGraph<SimpleModel>(x => x.Class);
            _ = childGraph2.HasMember(nameof(SimpleModel.Value1));
        }

        [Fact]
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

        [Fact]
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
            Assert.False(instanceGraph.HasMember(nameof(GraphModel.Prop1)));
            Assert.True(instanceGraph.HasMember(nameof(GraphModel.Array)));

            var instance2 = GraphModel.Create();
            var instanceGraph2 = graph.GetInstanceGraph(instance2);
            TestBasic(instanceGraph2);

            var childGraph = graph.GetChildGraph(nameof(GraphModel.Array));
            var childInstance = new SimpleModel() { Value1 = 1, Value2 = "2" };
            childGraph.AddInstanceGraph(childInstance, new Graph<SimpleModel>(x => x.Value2));

            var childInstanceGraph = graph.GetChildInstanceGraph(nameof(GraphModel.Array), childInstance);
            Assert.False(childInstanceGraph.HasMember(nameof(SimpleModel.Value1)));
            Assert.True(childInstanceGraph.HasMember(nameof(SimpleModel.Value2)));
        }

        [Fact]
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

        [Fact]
        public void Convert()
        {
            var graph = new Graph(nameof(GraphModel.Prop1));
            graph.AddChildGraph(nameof(GraphModel.Array), new Graph(nameof(SimpleModel.Value1)));
            graph.AddChildGraph(nameof(GraphModel.Class), new Graph(nameof(SimpleModel.Value1)));

            TestBasic(graph);

            var converted = new Graph<GraphModel>(graph);

            TestBasic(converted);
        }

        [Fact]
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

            Assert.True(graph1.Equals(graph2));
            Assert.False(graph1.Equals(graph3));

            Assert.True(graph1 == graph2);
            Assert.False(graph1 == graph3);
            Assert.True(graph1 != graph3);
        }

        //[Fact]
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

        //    Assert.Equal(model.BooleanThing, result.BooleanThing);
        //    Assert.NotEqual(model.ByteThing, result.ByteThing);

        //    Assert.NotNull(result.ClassThing);
        //    Assert.Equal(model.ClassThing.Value1, result.ClassThing.Value1);
        //    Assert.NotEqual(model.ClassThing.Value2, result.ClassThing.Value2);

        //    Assert.NotNull(result.ClassArray);
        //    Assert.Equal(model.ClassArray.Length, result.ClassArray.Length);
        //    Assert.NotEqual(model.ClassArray[0].Value1, result.ClassArray[0].Value1);
        //    Assert.Equal(model.ClassArray[0].Value2, result.ClassArray[0].Value2);
        //}

        [Fact]
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
            Assert.Equal(strCheck, str);
        }

        private void TestBasic(Graph graph)
        {
            //Validates graph only has these
            //x => x.Prop1,
            //x => x.Array.Select(x => x.Value1)
            //x => x.Class.Select(x => x.Value1)

            Assert.True(graph.HasMember(nameof(GraphModel.Prop1)));
            Assert.False(graph.HasMember(nameof(GraphModel.Prop2)));

            Assert.True(graph.HasMember(nameof(GraphModel.Array)));
            Assert.False(graph.HasMember(nameof(GraphModel.List)));

            var childGraph1 = graph.GetChildGraph<SimpleModel>(nameof(GraphModel.Array));
            Assert.NotNull(childGraph1);

            Assert.True(childGraph1.HasMember(nameof(SimpleModel.Value1)));
            Assert.False(childGraph1.HasMember(nameof(SimpleModel.Value2)));

            var childGraph2 = graph.GetChildGraph<SimpleModel>(nameof(GraphModel.List));
            Assert.Null(childGraph2);

            var childGraph3 = graph.GetChildGraph<SimpleModel>(nameof(GraphModel.Class));
            Assert.NotNull(childGraph3);
            Assert.True(childGraph3.HasMember(nameof(SimpleModel.Value1)));
        }
    }
}
