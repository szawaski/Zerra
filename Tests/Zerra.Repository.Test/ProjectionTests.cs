// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;
using Xunit;
using Zerra.Encryption;
using Zerra.Repository.Memory;

namespace Zerra.Repository.Test
{
    //projections of models and groups through IRepo on the memory engine, the event store, the layers, and the shared pieces; QueryParityTests covers every engine
    public class ProjectionTests
    {
        [Entity("ProjectionModel")]
        public sealed class Model
        {
            [Identity]
            public Guid ID { get; set; }
            public int Number { get; set; }
            public int? Maybe { get; set; }
            public decimal Money { get; set; }
            public bool Flag { get; set; }
            [StoreProperties(false, 50)]
            public string? Name { get; set; }
            [StoreProperties(false, 50)]
            public string? Note { get; set; }
            public Guid? ParentID { get; set; }
            [Relation(nameof(ParentID))]
            public Parent? Parent { get; set; }
        }

        [Entity("ProjectionParent")]
        public sealed class Parent
        {
            [Identity]
            public Guid ID { get; set; }
            [StoreProperties(false, 50)]
            public string? Name { get; set; }
            [Relation(nameof(Model.ParentID))]
            public Model[]? Children { get; set; }
        }

        public sealed class Row
        {
            public int Number { get; set; }
            public string? Name { get; set; }
            public string? ParentName { get; set; }
            public int Twice { get; set; }
            public long Siblings { get; set; }
        }

        public sealed class ParentRows
        {
            public string? Name { get; set; }
            public Row[]? Children { get; set; }
            public List<Row>? Sorted { get; set; }
            public IReadOnlyCollection<Row>? Some { get; set; }
            public IEnumerable<object>? Loose { get; set; }
        }
        public sealed class Summary
        {
            public bool Flag { get; set; }
            public int? Maybe { get; set; }
            public string? ParentName { get; set; }
            public int Rows { get; set; }
            public long Total { get; set; }
            public double Average { get; set; }
            public decimal Most { get; set; }
            public string? First { get; set; }
        }

        private static (IRepo Repo, Model[] Models, Parent[] Parents) MemoryRepo(MemoryEngine? engine = null)
        {
            engine ??= new MemoryEngine();
            var repo = Repo.New();
            repo.AddProvider(new TransactStoreProvider<Model>(engine));
            repo.AddProvider(new TransactStoreProvider<Parent>(engine));
            var parents = new[] { new Parent { ID = Guid.NewGuid(), Name = "pa" }, new Parent { ID = Guid.NewGuid(), Name = "pb" } };
            repo.Create<Parent>(parents);
            var names = new string?[] { "apple", "banana", null, "cherry", "date", "elder" };
            var models = new Model[6];
            for (var i = 0; i < models.Length; i++)
                models[i] = new Model { ID = Guid.NewGuid(), Number = i, Maybe = i % 2 == 0 ? i : null, Money = 1.5m * i, Flag = i % 3 == 0, Name = names[i], Note = "n", ParentID = parents[i % 2].ID };
            repo.Create<Model>(models);
            return (repo, models, parents);
        }

        [Fact]
        public async Task SelectMatchesLinq()
        {
            var (repo, models, parents) = MemoryRepo();
            var parentNames = parents.ToDictionary(x => x.ID, x => x.Name);

            var rows = repo.Select((Model x) => new Row { Number = x.Number, Name = x.Name, ParentName = x.Parent!.Name, Twice = x.Number * 2, Siblings = x.Parent!.Children!.Count() },
                x => x.Number > 0, QueryOrder<Model>.Create(x => x.Number, true), 1, 3);
            var expected = models.Where(x => x.Number > 0).OrderByDescending(x => x.Number).Skip(1).Take(3)
                .Select(x => (x.Number, x.Name, parentNames[x.ParentID!.Value], x.Number * 2, (long)models.Count(m => m.ParentID == x.ParentID))).ToArray();
            Assert.Equal(expected, rows.Select(r => (r.Number, r.Name, r.ParentName, r.Twice, r.Siblings)).ToArray());

            var all = await repo.SelectAsync((Model x) => new Row { Number = x.Number });
            Assert.Equal(models.Select(x => x.Number).Order(), all.Select(r => r.Number).Order());
            Assert.Empty(repo.Select((Model x) => new Row { Number = x.Number }, x => x.Number > 100));
        }

        [Fact]
        public async Task GroupMatchesLinq()
        {
            var (repo, models, parents) = MemoryRepo();
            var parentNames = parents.ToDictionary(x => x.ID, x => x.Name);

            var byFlag = repo.Group((Model x) => x.Flag, g => new Summary { Flag = g.Key, Rows = g.Count(), Total = g.Sum(x => x.Number), Average = g.Average(x => x.Number), Most = g.Max(x => x.Money), First = g.Min(x => x.Name) });
            foreach (var group in models.GroupBy(x => x.Flag))
            {
                var actual = byFlag.Single(r => r.Flag == group.Key);
                Assert.Equal(group.Count(), actual.Rows);
                Assert.Equal(group.Sum(x => x.Number), actual.Total);
                Assert.Equal(group.Average(x => x.Number), actual.Average);
                Assert.Equal(group.Max(x => x.Money), actual.Most);
                Assert.Equal(group.Min(x => x.Name), actual.First);
            }

            //a key of several members as an anonymous object, read back part by part
            var byPair = repo.Group((Model x) => new { x.Flag, x.Maybe }, g => new Summary { Flag = g.Key.Flag, Maybe = g.Key.Maybe, Rows = g.Count() });
            Assert.Equal(models.GroupBy(x => (x.Flag, x.Maybe)).Count(), byPair.Count);
            Assert.Equal(1, byPair.Single(r => r.Flag && r.Maybe == 0).Rows);
            Assert.Equal(2, byPair.Single(r => !r.Flag && r.Maybe is null).Rows);

            var byTuple = repo.Group((Model x) => new ValueTuple<bool, int?>(x.Flag, x.Maybe), g => new Summary { Flag = g.Key.Item1, Maybe = g.Key.Item2, Rows = g.Count() });
            Assert.Equal(byPair.Count, byTuple.Count);

            //a key through a relation
            var byParent = await repo.GroupAsync((Model x) => x.Parent!.Name, g => new Summary { ParentName = g.Key, Rows = g.Count(), Total = g.Sum(x => x.Number) });
            foreach (var group in models.GroupBy(x => parentNames[x.ParentID!.Value]))
            {
                var actual = byParent.Single(r => r.ParentName == group.Key);
                Assert.Equal(group.Count(), actual.Rows);
                Assert.Equal(group.Sum(x => x.Number), actual.Total);
            }
        }

        [Fact]
        public void HavingOrderAndPagingApplyToTheResults()
        {
            var (repo, models, _) = MemoryRepo();

            var top = repo.Group((Model x) => x.Maybe, g => new Summary { Maybe = g.Key, Rows = g.Count(), Total = g.Sum(x => x.Number) },
                having: r => r.Total > 0, order: QueryOrder<Summary>.Create(r => r.Total, true), skip: 1, take: 2);
            var expected = models.GroupBy(x => x.Maybe).Select(g => (g.Key, g.Count(), g.Sum(x => x.Number))).Where(x => x.Item3 > 0).OrderByDescending(x => x.Item3).Skip(1).Take(2).ToArray();
            Assert.Equal(expected, top.Select(r => (r.Maybe, r.Rows, (int)r.Total)).ToArray());

            //two flagged rows and four not, so ascending by count puts the flagged group first
            var ordered = repo.Group((Model x) => x.Flag, g => new Summary { Flag = g.Key, Rows = g.Count() }, order: QueryOrder<Summary>.Create(r => r.Rows, false, r => r.Flag));
            Assert.Equal([true, false], ordered.Select(r => r.Flag).ToArray());
            Assert.Equal([2, 4], ordered.Select(r => r.Rows).ToArray());
        }

        [Fact]
        public void ProjectionsAreChecked()
        {
            var (repo, _, _) = MemoryRepo();

            //a member must be the key, a part of it, Count, or an aggregate of a value
            _ = Assert.Throws<NotSupportedException>(() => repo.Group((Model x) => x.Flag, g => new Summary { Rows = g.Select(x => x.Number).Count() }));
            _ = Assert.Throws<NotSupportedException>(() => repo.Group((Model x) => x.Flag, g => new Summary { Total = g.First().Number }));
            _ = Assert.Throws<NotSupportedException>(() => repo.Group((Model x) => new { x.Flag, x.Maybe }, g => new Summary { Flag = g.Key.Flag, First = g.Key.ToString() }));
            //and the whole key of several parts can't be set to a member
            _ = Assert.Throws<NotSupportedException>(() => repo.Group((Model x) => new { x.Flag, x.Maybe }, g => new Summary { First = g.Key.ToString() }));
        }

        [Fact]
        public async Task CollectionsAreBuiltFromTheRelatedRows()
        {
            var (repo, models, parents) = MemoryRepo();

            var rows = await repo.SelectAsync((Parent x) => new ParentRows
            {
                Name = x.Name,
                Children = x.Children!.Select(c => new Row { Number = c.Number, Name = c.Name }).ToArray(),
                Sorted = x.Children!.OrderByDescending(c => c.Number).Select(c => new Row { Number = c.Number, Twice = c.Number * 2 }).ToList(),
                Some = x.Children!.Where(c => c.Flag).OrderBy(c => c.Number).Select(c => new Row { Number = c.Number }).ToList(),
            }, null, QueryOrder<Parent>.Create(x => x.Name));
            Assert.Equal(["pa", "pb"], rows.Select(r => r.Name));
            foreach (var row in rows)
            {
                var parent = parents.Single(p => p.Name == row.Name);
                var children = models.Where(m => m.ParentID == parent.ID).ToArray();
                Assert.Equal(children.Select(c => (c.Number, c.Name)).Order(), row.Children!.Select(c => (c.Number, c.Name)).Order());
                Assert.Equal(children.OrderByDescending(c => c.Number).Select(c => (c.Number, c.Number * 2)), row.Sorted!.Select(c => (c.Number, c.Twice)));
                Assert.Equal(children.Where(c => c.Flag).OrderBy(c => c.Number).Select(c => c.Number), row.Some!.Select(c => c.Number));
            }

            //a parent without rows gets an empty collection
            repo.Create(new Parent { ID = Guid.NewGuid(), Name = "pc" });
            var empty = repo.Select((Parent x) => new ParentRows { Children = x.Children!.Select(c => new Row { Number = c.Number }).ToArray() }, x => x.Name == "pc");
            Assert.Empty(Assert.Single(empty).Children!);

            //paging and skipping the rows, a result reading the model, and an element type the member doesn't take are refused
            _ = Assert.Throws<NotSupportedException>(() => repo.Select((Parent x) => new ParentRows { Children = x.Children!.Skip(1).Select(c => new Row { Number = c.Number }).ToArray() }));
            _ = Assert.Throws<NotSupportedException>(() => repo.Select((Parent x) => new ParentRows { Children = x.Children!.Select(c => new Row { Number = c.Number, ParentName = x.Name }).ToArray() }));
            _ = Assert.Throws<NotSupportedException>(() => repo.Select((Parent x) => new ParentRows { Children = x.Children!.Where(c => c.Name == x.Name).Select(c => new Row { Number = c.Number }).ToArray() }));
            _ = Assert.Throws<NotSupportedException>(() => repo.Select((Parent x) => new ParentRows { Loose = x.Children!.Select(c => new Row { Number = c.Number }).ToArray() }));
        }

        [Entity("ProjectionNode")]
        public sealed class Node
        {
            [Identity]
            public Guid ID { get; set; }
            public Guid? ParentID { get; set; }
            [StoreProperties(false, 50)]
            public string? Name { get; set; }
            [Relation(nameof(ParentID))]
            public Node? Parent { get; set; }
            [Relation(nameof(ParentID))]
            public Node[]? Children { get; set; }
        }
        public sealed class NodeRow
        {
            public string? Name { get; set; }
            public NodeRow[]? Children { get; set; }
        }

        [Fact]
        public void CollectionsWithinRowsStopAtAModelAlreadyProjected()
        {
            var repo = Repo.New();
            repo.AddProvider(new TransactStoreProvider<Node>(new MemoryEngine()));
            var root = new Node { ID = Guid.NewGuid(), Name = "root" };
            var children = new[] { new Node { ID = Guid.NewGuid(), ParentID = root.ID, Name = "c1" }, new Node { ID = Guid.NewGuid(), ParentID = root.ID, Name = "c2" } };
            repo.Create(root);
            repo.Create<Node>(children);
            repo.Create(new Node { ID = Guid.NewGuid(), ParentID = children[0].ID, Name = "g1" });

            var rows = repo.Select((Node x) => new NodeRow { Name = x.Name, Children = x.Children!.OrderBy(c => c.Name).Select(c => new NodeRow { Name = c.Name }).ToArray() }, x => x.Name == "root");
            Assert.Equal(["c1", "c2"], Assert.Single(rows).Children!.Select(c => c.Name));

            //the rows of a collection within a row can't be a row model above them, so a projection never follows a relation round and round; a model with a collection of itself nests one level
            var refused = Assert.Throws<NotSupportedException>(() => repo.Select((Node x) => new NodeRow { Name = x.Name, Children = x.Children!.Select(c => new NodeRow { Name = c.Name, Children = c.Children!.Select(g => new NodeRow { Name = g.Name }).ToArray() }).ToArray() }));
            Assert.Contains("already projected above them", refused.Message);
        }

        [Entity("ProjectionEvent")]
        public sealed class EventedModel
        {
            [Identity]
            public Guid ID { get; set; }
            public int Value { get; set; }
            public bool Flag { get; set; }
        }

        [Fact]
        public async Task EventStoreProjectsCurrentState()
        {
            var repo = Repo.New();
            repo.AddProvider(new EventStoreAsTransactStoreProvider<EventedModel>(new MemoryEngine()));
            var models = new[] { new EventedModel { ID = Guid.NewGuid(), Value = 1, Flag = true }, new EventedModel { ID = Guid.NewGuid(), Value = 2, Flag = true }, new EventedModel { ID = Guid.NewGuid(), Value = 3 } };
            repo.Create<EventedModel>(models);
            models[0].Value = 10;
            repo.Update(models[0]);
            var ids = models.Select(x => x.ID).ToArray();

            var rows = repo.Select((EventedModel x) => new Row { Number = x.Value }, x => ids.Contains(x.ID), QueryOrder<EventedModel>.Create(x => x.Value));
            Assert.Equal([2, 3, 10], rows.Select(r => r.Number).ToArray());

            var groups = await repo.GroupAsync((EventedModel x) => x.Flag, g => new Summary { Flag = g.Key, Rows = g.Count(), Total = g.Sum(x => x.Value) }, x => ids.Contains(x.ID));
            Assert.Equal(12, groups.Single(r => r.Flag).Total);
            Assert.Equal(2, groups.Single(r => r.Flag).Rows);
        }

        private sealed class EvenRules(ITransactStoreProvider<Model> next) : BaseTransactStoreRuleProvider<ITransactStoreProvider<Model>, Model>(next)
        {
            public override LambdaExpression? WhereExpression(Graph? graph) => (Expression<Func<Model, bool>>)(x => x.Number % 2 == 0);
        }

        private static readonly IEncryptor encryptor = new ZerraEncryptor("projection-test", SymmetricAlgorithmType.AES_GCM);

        private sealed class Encrypting(ITransactStoreProvider<Model> next) : BaseTransactStoreEncryptionProvider<ITransactStoreProvider<Model>, Model>(next)
        {
            public override IEncryptor Encryptor => encryptor;
            public override Graph<Model>? Properties => new(x => x.Note);
        }
        private sealed class Compressing(ITransactStoreProvider<Model> next) : BaseTransactStoreCompressionProvider<ITransactStoreProvider<Model>, Model>(next)
        {
            public override Graph<Model>? Properties => new(x => x.Note);
        }

        [Fact]
        public async Task Layers()
        {
            var engine = new MemoryEngine();
            var (_, models, _) = MemoryRepo(engine);

            var ruled = Repo.New();
            ruled.AddProvider<Model>(new EvenRules(new TransactStoreProvider<Model>(engine)));
            Assert.Equal(models.Where(x => x.Number % 2 == 0).Select(x => x.Number).Order(), ruled.Select((Model x) => new Row { Number = x.Number }).Select(r => r.Number).Order());
            Assert.Equal(models.Count(x => x.Number % 2 == 0 && x.Flag), (await ruled.GroupAsync((Model x) => x.Flag, g => new Summary { Flag = g.Key, Rows = g.Count() })).Single(r => r.Flag).Rows);

            foreach (var compression in new[] { false, true })
            {
                var layered = Repo.New();
                var root = new TransactStoreProvider<Model>(engine);
                layered.AddProvider<Model>(compression ? new Compressing(root) : new Encrypting(root));

                Assert.Equal(models.Length, layered.Select((Model x) => new Row { Number = x.Number, Name = x.Name }).Count);
                _ = Assert.Throws<NotSupportedException>(() => layered.Select((Model x) => new Row { Name = x.Note }));
                _ = Assert.Throws<NotSupportedException>(() => layered.Group((Model x) => x.Note, g => new Summary { First = g.Key }));
                _ = await Assert.ThrowsAsync<NotSupportedException>(() => layered.GroupAsync((Model x) => x.Flag, g => new Summary { First = g.Min(x => x.Note) }));
            }
        }

        [Fact]
        public void MembersAreReadFromTheProjection()
        {
            Expression<Func<IGrouping<bool, Model>, Summary>> select = g => new Summary { Flag = g.Key, Rows = g.Count(), Average = g.Average(x => x.Number), First = g.Min(x => x.Name) };
            Expression<Func<Model, bool>> groupBy = x => x.Flag;
            var members = select.GetProjectionMembers(groupBy);

            Assert.Equal([ProjectionMemberKind.KeyPart, ProjectionMemberKind.Count, ProjectionMemberKind.Average, ProjectionMemberKind.Min], members.Select(x => x.Kind));
            Assert.Equal(["Flag", "Rows", "Average", "First"], members.Select(x => x.MemberDetail.Name));
            Assert.Equal([typeof(bool), typeof(long), typeof(double), typeof(string)], members.Select(x => x.Type));
        }
    }
}
