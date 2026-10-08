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
            var graph = new Graph<GraphModel>(false);

            foreach (var member in typeof(GraphModel).GetMembers(BindingFlags.Public | BindingFlags.Instance))
            {
                Assert.False(graph.HasMember(member.Name));
                Assert.False(graph.HasMemberExplicitly(member.Name));
            }
            Assert.Null(graph.GetChildGraph(x => x.Class));

            graph = new Graph<GraphModel>(true);

            foreach (var member in typeof(GraphModel).GetMembers(BindingFlags.Public | BindingFlags.Instance))
            {
                Assert.True(graph.HasMember(member.Name));
                Assert.False(graph.HasMemberExplicitly(member.Name));
            }
            Assert.Null(graph.GetChildGraph(x => x.Class));
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
            Assert.True(childGraph1.HasMemberExplicitly(x => x.Value1));
            Assert.False(childGraph1.HasMember(x => x.Value2));
            Assert.False(childGraph1.HasMemberExplicitly(x => x.Value2));
            Assert.False(childGraph1.IncludeAllMembers);

            graph.AddMember(x => x.Array);

            childGraph1 = graph.GetChildGraph<SimpleModel>(x => x.Array);
            Assert.NotNull(childGraph1);
            Assert.True(childGraph1.HasMember(x => x.Value1));
            Assert.True(childGraph1.HasMemberExplicitly(x => x.Value1));
            Assert.True(childGraph1.HasMember(x => x.Value2));
            Assert.False(childGraph1.HasMemberExplicitly(x => x.Value2));
            Assert.True(childGraph1.IncludeAllMembers);
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
            Assert.False(childGraph1.HasMemberExplicitly(x => x.Value1));
            Assert.True(childGraph1.HasMember(x => x.Value2));
            Assert.True(childGraph1.HasMemberExplicitly(x => x.Value2));
            Assert.True(childGraph1.IncludeAllMembers);

            graph = new Graph<GraphModel>();
            graph.AddMember(x => x.Class.Value2);

            childGraph1 = graph.GetChildGraph<SimpleModel>(x => x.Class);
            Assert.NotNull(childGraph1);
            Assert.False(childGraph1.HasMember(x => x.Value1));
            Assert.False(childGraph1.HasMemberExplicitly(x => x.Value1));
            Assert.True(childGraph1.HasMember(x => x.Value2));
            Assert.True(childGraph1.HasMemberExplicitly(x => x.Value2));
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
            Assert.False(childGraph1.HasMemberExplicitly(x => x.Value1));
            Assert.False(childGraph1.HasMember(x => x.Value2));
            Assert.False(childGraph1.HasMemberExplicitly(x => x.Value2));
            Assert.False(childGraph1.IncludeAllMembers);
            Assert.True(graph.HasMember(x => x.Prop1));
            Assert.False(graph.HasMemberExplicitly(x => x.Prop1));

            graph.AddMember(x => x.Class.Value2);
            childGraph1 = graph.GetChildGraph<SimpleModel>(x => x.Class);
            Assert.False(childGraph1.HasMember(x => x.Value1));
            Assert.False(childGraph1.HasMemberExplicitly(x => x.Value1));
            Assert.True(childGraph1.HasMember(x => x.Value2));
            Assert.True(childGraph1.HasMemberExplicitly(x => x.Value2));
            Assert.False(childGraph1.IncludeAllMembers);

            graph.AddMember(x => x.Class);
            childGraph1 = graph.GetChildGraph<SimpleModel>(x => x.Class);
            Assert.False(childGraph1.HasMember(x => x.Value1));
            Assert.False(childGraph1.HasMemberExplicitly(x => x.Value1));
            Assert.True(childGraph1.HasMember(x => x.Value2));
            Assert.True(childGraph1.HasMemberExplicitly(x => x.Value2));
            Assert.True(childGraph1.IncludeAllMembers);

            graph = new Graph<GraphModel>(false);

            graph.RemoveMember(x => x.Class.Value1);

            childGraph1 = graph.GetChildGraph<SimpleModel>(x => x.Class);
            Assert.NotNull(childGraph1);
            Assert.False(childGraph1.HasMember(x => x.Value1));
            Assert.False(childGraph1.HasMemberExplicitly(x => x.Value1));
            Assert.False(childGraph1.HasMember(x => x.Value2));
            Assert.False(childGraph1.HasMemberExplicitly(x => x.Value2));
            Assert.False(childGraph1.IncludeAllMembers);
            Assert.False(graph.HasMember(x => x.Prop1));
            Assert.False(graph.HasMemberExplicitly(x => x.Prop1));

            graph.AddMember(x => x.Class.Value2);
            childGraph1 = graph.GetChildGraph<SimpleModel>(x => x.Class);
            Assert.False(childGraph1.HasMember(x => x.Value1));
            Assert.False(childGraph1.HasMemberExplicitly(x => x.Value1));
            Assert.True(childGraph1.HasMember(x => x.Value2));
            Assert.True(childGraph1.HasMemberExplicitly(x => x.Value2));
            Assert.False(childGraph1.IncludeAllMembers);

            graph.AddMember(x => x.Class);
            childGraph1 = graph.GetChildGraph<SimpleModel>(x => x.Class);
            Assert.False(childGraph1.HasMember(x => x.Value1));
            Assert.False(childGraph1.HasMemberExplicitly(x => x.Value1));
            Assert.True(childGraph1.HasMember(x => x.Value2));
            Assert.True(childGraph1.HasMemberExplicitly(x => x.Value2));
            Assert.True(childGraph1.IncludeAllMembers);

            graph = new Graph<GraphModel>(false, x => x.Class);

            graph.RemoveMember(x => x.Class.Value1);

            childGraph1 = graph.GetChildGraph<SimpleModel>(x => x.Class);
            Assert.NotNull(childGraph1);
            Assert.False(childGraph1.HasMember(x => x.Value1));
            Assert.False(childGraph1.HasMemberExplicitly(x => x.Value1));
            Assert.True(childGraph1.HasMember(x => x.Value2));
            Assert.False(childGraph1.HasMemberExplicitly(x => x.Value2));
            Assert.True(childGraph1.IncludeAllMembers);
            Assert.False(graph.HasMember(x => x.Prop1));
            Assert.False(graph.HasMemberExplicitly(x => x.Prop1));

            graph.AddMember(x => x.Class.Value2);
            childGraph1 = graph.GetChildGraph<SimpleModel>(x => x.Class);
            Assert.False(childGraph1.HasMember(x => x.Value1));
            Assert.False(childGraph1.HasMemberExplicitly(x => x.Value1));
            Assert.True(childGraph1.HasMember(x => x.Value2));
            Assert.True(childGraph1.HasMemberExplicitly(x => x.Value2));
            Assert.True(childGraph1.IncludeAllMembers);

            graph.AddMember(x => x.Class);
            childGraph1 = graph.GetChildGraph<SimpleModel>(x => x.Class);
            Assert.False(childGraph1.HasMember(x => x.Value1));
            Assert.False(childGraph1.HasMemberExplicitly(x => x.Value1));
            Assert.True(childGraph1.HasMember(x => x.Value2));
            Assert.True(childGraph1.HasMemberExplicitly(x => x.Value2));
            Assert.True(childGraph1.IncludeAllMembers);
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

        [Fact]
        public void ParseSignatureEmpty()
        {
            Assert.True(Graph.TryParseSignature(String.Empty, out var graph));
            Assert.True(graph.IsEmpty);
            Assert.Equal(String.Empty, graph.Signature);
            Assert.Equal(new Graph(), graph);
        }

        [Fact]
        public void ParseSignatureAllMembers()
        {
            var graph = new Graph(true);
            Assert.Equal("A:", graph.Signature);

            Assert.True(Graph.TryParseSignature(graph.Signature, out var parsed));
            Assert.True(parsed.IncludeAllMembers);
            Assert.Equal(graph, parsed);
        }

        [Fact]
        public void ParseSignatureMembers()
        {
            var graph = new Graph("Prop1", "Prop2");
            graph.RemoveMember("Prop3");

            Assert.True(Graph.TryParseSignature(graph.Signature, out var parsed));

            Assert.Equal(graph, parsed);
            Assert.True(parsed.HasMember("Prop1"));
            Assert.True(parsed.HasMember("Prop2"));
            Assert.False(parsed.HasMember("Prop3"));
            Assert.True(parsed.HasRemovedMembers);
            Assert.Equal<string>(["Prop1", "Prop2"], parsed.ExplicitMembers.Order());
        }

        [Fact]
        public void ParseSignatureChildGraphs()
        {
            var graph = new Graph<GraphModel>(
                x => x.Prop1,
                x => x.Array.Select(x => x.Value1),
                x => x.Class.Value1
            );

            Assert.True(Graph.TryParseSignature(graph.Signature, out var parsed));

            Assert.Equal(graph.Signature, parsed.Signature);
            TestBasic(parsed);
        }

        [Fact]
        public void ParseSignatureNestedChildGraphs()
        {
            var graph = new Graph<GraphModel>(
                x => x.Nested.Class.Value1,
                x => x.Nested.Prop2
            );
            graph.RemoveMember("Prop1");

            Assert.True(Graph.TryParseSignature(graph.Signature, out var parsed));

            Assert.Equal(graph.Signature, parsed.Signature);
            var nested = parsed.GetChildGraph("Nested");
            Assert.NotNull(nested);
            Assert.True(nested.HasMember("Prop2"));
            var nestedClass = nested.GetChildGraph("Class");
            Assert.NotNull(nestedClass);
            Assert.True(nestedClass.HasMember("Value1"));
            Assert.False(parsed.HasMember("Prop1"));
        }

        [Fact]
        public void ParseSignatureIntoExistingGraph()
        {
            var graph = new Graph<GraphModel>(x => x.Prop2);
            Assert.True(Graph.TryParseSignature(new Graph<GraphModel>(x => x.Prop1).Signature, graph));

            //the members it had are replaced, not merged
            Assert.True(graph.HasMember("Prop1"));
            Assert.False(graph.HasMember("Prop2"));
        }

        [Theory]
        [InlineData("X")]
        [InlineData("X:")]
        [InlineData("P:Prop1:")]
        [InlineData("G:Child")]
        [InlineData("G:Child:(P:Value1")]
        [InlineData("G:Child:()G:Child:()")]
        public void ParseSignatureInvalid(string signature)
        {
            Assert.False(Graph.TryParseSignature(signature, out var parsed));
            Assert.Null(parsed);

            //an existing graph is left empty
            var graph = new Graph<GraphModel>(x => x.Prop1);
            Assert.False(Graph.TryParseSignature(signature, graph));
            Assert.True(graph.IsEmpty);
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

        [Fact]
        public void RemoveThenAdd_MatchesAdd()
        {
            var added = new Graph("A");
            var readded = new Graph();
            readded.RemoveMember("A");
            Assert.True(readded.HasRemovedMembers);
            readded.AddMember("A");

            Assert.False(readded.HasRemovedMembers);
            Assert.True(readded.HasMember("A"));
            Assert.Equal(added.Signature, readded.Signature);
            Assert.Equal(added, readded);
            Assert.Equal(added.GetHashCode(), readded.GetHashCode());
        }

        [Fact]
        public void State()
        {
            var graph = new Graph();
            Assert.True(graph.IsEmpty);
            Assert.False(graph.HasAddedMembers);
            Assert.False(graph.HasRemovedMembers);
            Assert.Equal("", graph.Signature);

            graph.IncludeAllMembers = true;
            Assert.False(graph.IsEmpty);
            Assert.True(graph.HasMember("Anything"));
            Assert.Equal("A:", graph.Signature);

            graph.RemoveMembers("B", "C");
            Assert.True(graph.HasRemovedMembers);
            Assert.False(graph.HasMember("B"));
            Assert.True(graph.HasMember("D"));
            Assert.False(graph.HasMemberExplicitly("D"));

            graph.IncludeAllMembers = false;
            graph.AddMembers(["D"]);
            Assert.True(graph.HasAddedMembers);
            Assert.True(graph.HasMemberExplicitly("D"));
            Assert.False(graph.HasMember("E"));

            Assert.False(graph.Equals("not a graph"));
            Assert.NotEqual(new Graph("X"), new Graph("Y"));
        }

        [Fact]
        public void ChildGraphsAndInstances()
        {
            var graph = new Graph("Child");
            graph.AddChildGraph("Child", new Graph("Inner"));
            Assert.True(graph.GetChildGraph("Child")!.IncludeAllMembers);
            Assert.Null(graph.GetChildGraph("Missing"));

            graph.AddOrReplaceChildGraph("Child", new Graph("Other"));
            Assert.True(graph.GetChildGraph("Child")!.HasMember("Other"));
            Assert.False(graph.GetChildGraph("Child")!.HasMember("Inner"));

            graph.RemoveMember("Child");
            Assert.Null(graph.GetChildGraph("Child"));
            Assert.False(graph.HasMember("Child"));

            var instance = new object();
            graph.AddInstanceGraph(instance, new Graph("X"));
            _ = Assert.Throws<InvalidOperationException>(() => graph.Signature);
            var shared = new Graph("Y");
            graph.AddInstanceGraph(new object(), shared);
            _ = Assert.Throws<InvalidOperationException>(() => new Graph().AddInstanceGraph(new object(), shared));
            graph.RemoveInstanceGraph(instance);
            new Graph().RemoveInstanceGraph(instance);
        }

        [Fact]
        public void InvalidArguments()
        {
            var graph = new Graph();
            _ = Assert.Throws<ArgumentNullException>(() => graph.AddMember(""));
            _ = Assert.Throws<ArgumentNullException>(() => graph.RemoveMember(" "));
            _ = Assert.Throws<ArgumentNullException>(() => graph.AddMembers((IEnumerable<string>)null!));
            _ = Assert.Throws<ArgumentNullException>(() => graph.RemoveMembers((IEnumerable<string>)null!));
            _ = Assert.Throws<InvalidOperationException>(() => graph.AddChildGraph("", new Graph()));
            _ = Assert.Throws<InvalidOperationException>(() => graph.AddOrReplaceChildGraph("", new Graph()));
            Assert.False(Graph.TryParseSignature(null, out _));
            _ = Assert.Throws<ArgumentNullException>(() => Graph.TryParseSignature("A:", (Graph)null!));
        }

        [Fact]
        public void AddChildGraph_Again_Merges()
        {
            var graph = new Graph();
            graph.AddChildGraph("Child", new Graph("A", "B"));

            var more = new Graph(true, ["C"]);
            more.RemoveMember("B");
            more.AddChildGraph("Inner", new Graph("X"));
            var instance = new object();
            more.AddInstanceGraph(instance, new Graph("Y"));
            graph.AddChildGraph("Child", more);

            var child = graph.GetChildGraph("Child")!;
            Assert.True(child.IncludeAllMembers);
            Assert.True(child.HasMember("A"));
            Assert.True(child.HasMember("C"));
            Assert.False(child.HasMember("B"));
            Assert.True(child.GetChildGraph("Inner")!.HasMember("X"));
            Assert.True(child.GetInstanceGraph(instance).HasMember("Y"));

            //a member added plainly becomes the whole child when it's given a child graph
            var replaced = new Graph("Child");
            replaced.AddOrReplaceChildGraph("Child", new Graph("A"));
            Assert.True(replaced.GetChildGraph("Child")!.IncludeAllMembers);
        }

        [Fact]
        public void GetChildInstanceGraph_Generic()
        {
            var graph = new Graph<GraphModel>(x => x.Class.Value1);
            Assert.Null(new Graph<GraphModel>().GetChildInstanceGraph<SimpleModel>(nameof(GraphModel.Class), new object()));
            Assert.Null(new Graph<GraphModel>().GetChildInstanceGraph(nameof(GraphModel.Class), new object()));
            Assert.Null(new Graph<GraphModel>().GetChildGraph<SimpleModel>(nameof(GraphModel.Class)));
            Assert.Null(graph.GetChildInstanceGraph<SimpleModel>("Missing", new object()));
            Assert.Null(graph.GetChildInstanceGraph("Missing", new object()));

            //without an instance graph it is the child graph
            var child = graph.GetChildInstanceGraph<SimpleModel>(nameof(GraphModel.Class), new object())!;
            Assert.True(child.HasMember(x => x.Value1));

            //an untyped child is converted
            var untyped = new Graph<GraphModel>();
            untyped.AddChildGraph(nameof(GraphModel.Class), new Graph(nameof(SimpleModel.Value2)));
            Assert.True(untyped.GetChildInstanceGraph<SimpleModel>(nameof(GraphModel.Class), new object())!.HasMember(x => x.Value2));

            //an instance graph is used for its instance, typed or not
            var instance = new SimpleModel();
            graph.GetChildGraph(nameof(GraphModel.Class))!.AddInstanceGraph(instance, new Graph<SimpleModel>(x => x.Value2));
            Assert.True(graph.GetChildInstanceGraph<SimpleModel>(nameof(GraphModel.Class), instance)!.HasMember(x => x.Value2));
            var other = new SimpleModel();
            graph.GetChildGraph(nameof(GraphModel.Class))!.AddInstanceGraph(other, new Graph(nameof(SimpleModel.Value1)));
            Assert.True(graph.GetChildInstanceGraph<SimpleModel>(nameof(GraphModel.Class), other)!.HasMember(x => x.Value1));
        }

        [Fact]
        public void ToString_AllMembersNestedAndInstances()
        {
            var graph = new Graph<GraphModel>(true, x => x.Nested.Class.Value1);
            var nl = Environment.NewLine;
            Assert.Equal($"[ALL]{nl}Nested{nl}  Class{nl}    Value1", graph.ToString());

            graph.AddInstanceGraph(new object(), new Graph());
            Assert.Equal("Graph has instances", graph.ToString());
        }

        [Fact]
        public void InvalidMemberExpressions_Throw()
        {
            _ = Assert.Throws<ArgumentException>(() => new Graph<GraphModel>(x => 5));
            _ = Assert.Throws<ArgumentException>(() => new Graph<GraphModel>(x => x.Class.ToString()));
            _ = Assert.Throws<ArgumentException>(() => new Graph<GraphModel>(x => x.Array.Select(y => y.Value1).ToArray()));
            _ = Assert.Throws<ArgumentException>(() => new Graph<GraphModel>(x => DateTime.Now));
            _ = Assert.Throws<ArgumentException>(() => new Graph<GraphModel>(x => x.Array.Select(y => 5)));
        }

        [Fact]
        public void Typed_RemovedChildAndNullArguments()
        {
            //once a child is removed, members under it are left out instead of bringing it back
            var graph = new Graph<GraphModel>(true);
            graph.RemoveMember(x => x.Class);
            graph.AddMembers(x => x.Class.Value1);
            graph.AddMember(x => x.Class.Value2);
            graph.RemoveMembers(x => x.Class.Value1);
            graph.RemoveMember(x => x.Class.Value2);
            graph.AddChildGraph(x => x.Class.Value1, new Graph());
            graph.AddOrReplaceChildGraph(x => x.Class.Value1, new Graph());
            Assert.False(graph.HasMember(x => x.Class));
            Assert.False(graph.HasMember(x => x.Class.Value1));
            Assert.False(graph.HasMemberExplicitly(x => x.Class.Value1));
            Assert.True(graph.HasMember(x => x.Prop1));

            graph.AddOrReplaceChildGraph(x => x.Nested, new Graph<GraphModel>(x => x.Prop2));
            Assert.True(graph.GetChildGraph<GraphModel>(x => x.Nested)!.HasMember(x => x.Prop2));
            graph.AddOrReplaceChildGraph(x => x.Nested, new Graph<GraphModel>(x => x.Prop1));
            Assert.False(graph.GetChildGraph<GraphModel>(x => x.Nested)!.HasMember(x => x.Prop2));
            Assert.Null(new Graph<GraphModel>().GetChildGraph(x => x.Class));

            _ = Assert.Throws<ArgumentNullException>(() => graph.AddMembers((IEnumerable<System.Linq.Expressions.Expression<Func<GraphModel, object?>>>)null!));
            _ = Assert.Throws<ArgumentNullException>(() => graph.RemoveMembers((IEnumerable<System.Linq.Expressions.Expression<Func<GraphModel, object?>>>)null!));
            _ = Assert.Throws<ArgumentNullException>(() => graph.AddMember((System.Linq.Expressions.Expression<Func<GraphModel, object?>>)null!));
            _ = Assert.Throws<ArgumentNullException>(() => graph.RemoveMember((System.Linq.Expressions.Expression<Func<GraphModel, object?>>)null!));
            _ = Assert.Throws<ArgumentNullException>(() => graph.AddChildGraph((System.Linq.Expressions.Expression<Func<GraphModel, object?>>)null!, new Graph()));
            _ = Assert.Throws<ArgumentNullException>(() => graph.AddOrReplaceChildGraph((System.Linq.Expressions.Expression<Func<GraphModel, object?>>)null!, new Graph()));
            _ = Assert.Throws<ArgumentNullException>(() => graph.HasMember((System.Linq.Expressions.Expression<Func<GraphModel, object?>>)null!));
            _ = Assert.Throws<ArgumentNullException>(() => graph.HasMemberExplicitly((System.Linq.Expressions.Expression<Func<GraphModel, object?>>)null!));
        }
    }
}
