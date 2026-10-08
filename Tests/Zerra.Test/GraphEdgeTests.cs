// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using Zerra.Test.Helpers.Models;

namespace Zerra.Test
{
    public class GraphEdgeTests
    {
        [Fact]
        public void Constructors_AndEquality()
        {
            Assert.True(new Graph((Graph?)null).IsEmpty);
            Assert.True(new Graph((IEnumerable<string>?)["A", "B"]).HasMember("B"));
            Assert.True(new Graph(true, (IEnumerable<string>?)["A"]).IncludeAllMembers);

            Graph? none = null;
            var graph = new Graph("A");
            Assert.False(none == graph);
            Assert.False(graph == none);
            Assert.True(none == null);
            Assert.True(graph == new Graph("A"));
        }

        [Fact]
        public void ToString_SkipsRemovedMembersAndIndentsChildren()
        {
            var graph = new Graph("A", "B");
            graph.RemoveMember("B");
            graph.AddChildGraph("Child", new Graph(true, "C"));

            var lines = graph.ToString().Split(Environment.NewLine);
            Assert.Equal(["A", "Child", "  [ALL]", "  C"], lines);
        }

        [Fact]
        public void GetChildGraph_MissingChild_IsNull()
        {
            var graph = new Graph<GraphModel>(x => x.Prop1);
            Assert.Null(graph.GetChildGraph(x => x.Class));
            Assert.Null(graph.GetChildGraph<SimpleModel>(x => x.Class));
            Assert.Null(graph.GetChildGraph(x => x.Nested.Class));
            Assert.Null(graph.GetChildGraph(x => x.Array.Select(y => y.Value1)));
        }

        [Fact]
        public void GetChildInstanceGraph_TypedChild_IsReturned()
        {
            var graph = new Graph<GraphModel>(x => x.Prop1);
            var child = new Graph<SimpleModel>(x => x.Value1);
            graph.AddChildGraph(nameof(GraphModel.Class), child);
            Assert.Same(child, graph.GetChildInstanceGraph<SimpleModel>(nameof(GraphModel.Class), new SimpleModel()));
        }

        [Fact]
        public void MemberAddedThenUsedAsAChild_IncludesAllItsMembers()
        {
            var graph = new Graph<GraphModel>(x => x.Class.Value1);
            graph.AddMember(nameof(GraphModel.Class));
            graph.AddMember(x => x.Class.Value2);
            Assert.True(graph.GetChildGraph(x => x.Class)!.IncludeAllMembers);
        }

        private static readonly SimpleModel[] staticArray = [];

        [Fact]
        public void InvalidMemberExpressions_Throw()
        {
            _ = Assert.Throws<ArgumentException>(() => new Graph<GraphModel>(x => x.Prop1.ToString()));
            _ = Assert.Throws<ArgumentException>(() => new Graph<GraphModel>(x => x.Array.Where(y => true).Select(y => y.Value1)));
            Func<SimpleModel, int> selector = y => y.Value1;
            _ = Assert.Throws<ArgumentException>(() => new Graph<GraphModel>(x => x.Array.Select(selector)));
            _ = Assert.Throws<ArgumentException>(() => new Graph<GraphModel>(x => staticArray.Select(y => y.Value1)));
            _ = Assert.Throws<ArgumentException>(() => new Graph<GraphModel>(x => 5));
        }

        [Flags]
        public enum UShortFlags : ushort { None = 0, A = 1, B = 2 }
        [Flags]
        public enum UIntFlags : uint { None = 0, A = 1, B = 2 }

        [Fact]
        public void EnumName_ParsesFlagsOfUnsignedTypes()
        {
            Assert.Equal(UShortFlags.A | UShortFlags.B, EnumName.Parse<UShortFlags>("A|B"));
            Assert.Equal(UIntFlags.A | UIntFlags.B, EnumName.Parse<UIntFlags>("A|B"));
            Assert.Equal(UShortFlags.A | UShortFlags.B, EnumName.Parse("A|B", typeof(UShortFlags)));
            Assert.Equal(UIntFlags.A | UIntFlags.B, EnumName.Parse("A|B", typeof(UIntFlags)));
        }
    }
}
