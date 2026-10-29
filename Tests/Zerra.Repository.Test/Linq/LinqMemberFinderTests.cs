// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;
using Xunit;
using Zerra.Reflection;
using Zerra.Repository.Linq;

namespace Zerra.Repository.Test.Linq
{
    public sealed class LinqMemberFinderTests
    {
        private sealed class Parent
        {
            public string? Name { get; set; }
        }
        private sealed class Item
        {
            public string? Name { get; set; }
        }
        private sealed class Model
        {
            public int Number { get; set; }
            public string? Name { get; set; }
            public Parent? Parent { get; set; }
            public Item[]? Items { get; set; }
        }

        private static MemberDetail[] Members(params string[] names)
            => TypeAnalyzer.GetTypeDetail(typeof(Model)).Members.Where(x => names.Contains(x.Name)).ToArray();

        private static bool Reads(Expression<Func<Model, object?>> expression, params string[] names) => LinqMemberFinder.ReadsMember(expression, Members(names));

        [Fact]
        public void FindsAMemberOfTheParameter()
        {
            Assert.True(Reads(x => x.Name, "Name"));
            Assert.True(Reads(x => x.Number > 1 && x.Name == "a", "Name"));
            Assert.True(Reads(x => x.Name!.Length + x.Number, "Number"));
            Assert.True(Reads(x => new { x.Number, x.Name }, "Name"));
            Assert.True(Reads(x => x.Name, "Number", "Name"));
        }

        [Fact]
        public void IgnoresOtherMembers()
        {
            Assert.False(Reads(x => x.Number, "Name"));
            Assert.False(Reads(x => x.Number > 1, "Name", "Parent"));
        }

        [Fact]
        public void OnlyTheParameterCounts()
        {
            //the related model's Name isn't this model's Name, the relation itself is what is read
            Assert.False(Reads(x => x.Parent!.Name, "Name"));
            Assert.True(Reads(x => x.Parent!.Name, "Parent"));

            //a nested lambda's parameter isn't the root
            Assert.False(Reads(x => x.Items!.Any(i => i.Name == "a"), "Name"));
            Assert.True(Reads(x => x.Items!.Any(i => i.Name == "a"), "Items"));

            //a captured variable's member isn't the model's
            var other = new Model { Name = "a" };
            Assert.False(Reads(x => x.Number == other.Name!.Length, "Name"));
        }

        private sealed class Summary
        {
            public int Value { get; set; }
        }

        [Fact]
        public void ByParameterType_LooksInsideNestedLambdas()
        {
            Expression<Func<IGrouping<int, Model>, Summary>> sums = g => new Summary { Value = g.Sum(x => x.Name!.Length) };
            Expression<Func<IGrouping<int, Model>, Summary>> counts = g => new Summary { Value = g.Count() };
            Expression<Func<Model, Summary>> direct = x => new Summary { Value = x.Number };

            Assert.True(LinqMemberFinder.ReadsMember(sums, typeof(Model), Members("Name")));
            Assert.False(LinqMemberFinder.ReadsMember(sums, typeof(Model), Members("Number")));
            Assert.False(LinqMemberFinder.ReadsMember(counts, typeof(Model), Members("Name")));
            Assert.True(LinqMemberFinder.ReadsMember(direct, typeof(Model), Members("Number")));
            Assert.False(LinqMemberFinder.ReadsMember(sums, typeof(Model), []));
        }

        [Fact]
        public void NothingToFind()
        {
            Assert.False(LinqMemberFinder.ReadsMember(null, Members("Name")));
            Assert.False(Reads(x => x.Name));
        }
    }
}
