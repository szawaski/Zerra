// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;
using Xunit;
using Zerra.Encryption;
using Zerra.Repository.Linq;
using Zerra.Repository.Memory;
using Zerra.Repository.Reflection;

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

            var rows = repo.Query<Model>().Where(x => x.Number > 0).OrderByDescending(x => x.Number).Skip(1).Take(3).Select(x => new Row { Number = x.Number, Name = x.Name, ParentName = x.Parent!.Name, Twice = x.Number * 2, Siblings = x.Parent!.Children!.Count() }).ToArray();
            var expected = models.Where(x => x.Number > 0).OrderByDescending(x => x.Number).Skip(1).Take(3)
                .Select(x => (x.Number, x.Name, parentNames[x.ParentID!.Value], x.Number * 2, (long)models.Count(m => m.ParentID == x.ParentID))).ToArray();
            Assert.Equal(expected, rows.Select(r => (r.Number, r.Name, r.ParentName, r.Twice, r.Siblings)).ToArray());

            var all = await repo.Query<Model>().Select(x => new Row { Number = x.Number }).ToArrayAsync(cancellationToken: TestContext.Current.CancellationToken);
            Assert.Equal(models.Select(x => x.Number).Order(), all.Select(r => r.Number).Order());
            Assert.Empty(repo.Query<Model>().Where(x => x.Number > 100).Select(x => new Row { Number = x.Number }).ToArray());
        }

        [Fact]
        public async Task GroupMatchesLinq()
        {
            var (repo, models, parents) = MemoryRepo();
            var parentNames = parents.ToDictionary(x => x.ID, x => x.Name);

            var byFlag = repo.Query<Model>().GroupBy(x => x.Flag).Select(g => new Summary { Flag = g.Key, Rows = g.Count(), Total = g.Sum(x => x.Number), Average = g.Average(x => x.Number), Most = g.Max(x => x.Money), First = g.Min(x => x.Name) }).ToArray();
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
            var byPair = repo.Query<Model>().GroupBy(x => new { x.Flag, x.Maybe }).Select(g => new Summary { Flag = g.Key.Flag, Maybe = g.Key.Maybe, Rows = g.Count() }).ToArray();
            Assert.Equal(models.GroupBy(x => (x.Flag, x.Maybe)).Count(), byPair.Length);
            Assert.Equal(1, byPair.Single(r => r.Flag && r.Maybe == 0).Rows);
            Assert.Equal(2, byPair.Single(r => !r.Flag && r.Maybe is null).Rows);

            var byTuple = repo.Query<Model>().GroupBy(x => new ValueTuple<bool, int?>(x.Flag, x.Maybe)).Select(g => new Summary { Flag = g.Key.Item1, Maybe = g.Key.Item2, Rows = g.Count() }).ToArray();
            Assert.Equal(byPair.Length, byTuple.Length);

            //a key through a relation
            var byParent = await repo.Query<Model>().GroupBy(x => x.Parent!.Name).Select(g => new Summary { ParentName = g.Key, Rows = g.Count(), Total = g.Sum(x => x.Number) }).ToArrayAsync(cancellationToken: TestContext.Current.CancellationToken);
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

            var top = repo.Query<Model>().GroupBy(x => x.Maybe).Select(g => new Summary { Maybe = g.Key, Rows = g.Count(), Total = g.Sum(x => x.Number) }).Where(r => r.Total > 0).OrderByDescending(r => r.Total).Skip(1).Take(2).ToArray();
            var expected = models.GroupBy(x => x.Maybe).Select(g => (g.Key, g.Count(), g.Sum(x => x.Number))).Where(x => x.Item3 > 0).OrderByDescending(x => x.Item3).Skip(1).Take(2).ToArray();
            Assert.Equal(expected, top.Select(r => (r.Maybe, r.Rows, (int)r.Total)).ToArray());

            //two flagged rows and four not, so ascending by count puts the flagged group first
            var ordered = repo.Query<Model>().GroupBy(x => x.Flag).Select(g => new Summary { Flag = g.Key, Rows = g.Count() }).OrderBy(r => r.Rows).ThenBy(r => r.Flag).ToArray();
            Assert.Equal([true, false], ordered.Select(r => r.Flag).ToArray());
            Assert.Equal([2, 4], ordered.Select(r => r.Rows).ToArray());
        }

        [Fact]
        public void ProjectionsAreChecked()
        {
            var (repo, _, _) = MemoryRepo();

            //a member must be the key, a part of it, Count, or an aggregate of a value
            _ = Assert.Throws<NotSupportedException>(() => repo.Query<Model>().GroupBy(x => x.Flag).Select(g => new Summary { Rows = g.Select(x => x.Number).Count() }).ToArray());
            _ = Assert.Throws<NotSupportedException>(() => repo.Query<Model>().GroupBy(x => x.Flag).Select(g => new Summary { Total = g.First().Number }).ToArray());
            _ = Assert.Throws<NotSupportedException>(() => repo.Query<Model>().GroupBy(x => new { x.Flag, x.Maybe }).Select(g => new Summary { Flag = g.Key.Flag, First = g.Key.ToString() }).ToArray());
            //and the whole key of several parts can't be set to a member
            _ = Assert.Throws<NotSupportedException>(() => repo.Query<Model>().GroupBy(x => new { x.Flag, x.Maybe }).Select(g => new Summary { First = g.Key.ToString() }).ToArray());
        }

        [Fact]
        public async Task CollectionsAreBuiltFromTheRelatedRows()
        {
            var (repo, models, parents) = MemoryRepo();

            var rows = await repo.Query<Parent>().OrderBy(x => x.Name).Select(x => new ParentRows
            {
                Name = x.Name,
                Children = x.Children!.Select(c => new Row { Number = c.Number, Name = c.Name }).ToArray(),
                Sorted = x.Children!.OrderByDescending(c => c.Number).Select(c => new Row { Number = c.Number, Twice = c.Number * 2 }).ToList(),
                Some = x.Children!.Where(c => c.Flag).OrderBy(c => c.Number).Select(c => new Row { Number = c.Number }).ToList(),
            }).ToArrayAsync(cancellationToken: TestContext.Current.CancellationToken);
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
            var empty = repo.Query<Parent>().Where(x => x.Name == "pc").Select(x => new ParentRows { Children = x.Children!.Select(c => new Row { Number = c.Number }).ToArray() }).ToArray();
            Assert.Empty(Assert.Single(empty).Children!);

            //a result reading the model is refused
            _ = Assert.Throws<NotSupportedException>(() => repo.Query<Parent>().Select(x => new ParentRows { Children = x.Children!.Select(c => new Row { Number = c.Number, ParentName = x.Name }).ToArray() }).ToArray());
            _ = Assert.Throws<NotSupportedException>(() => repo.Query<Parent>().Select(x => new ParentRows { Children = x.Children!.Where(c => c.Name == x.Name).Select(c => new Row { Number = c.Number }).ToArray() }).ToArray());
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

            var rows = repo.Query<Node>().Where(x => x.Name == "root").Select(x => new NodeRow { Name = x.Name, Children = x.Children!.OrderBy(c => c.Name).Select(c => new NodeRow { Name = c.Name }).ToArray() }).ToArray();
            Assert.Equal(["c1", "c2"], Assert.Single(rows).Children!.Select(c => c.Name));

            //the rows of a collection within a row can't be a row model above them, so a projection never follows a relation round and round; a model with a collection of itself nests one level
            var refused = Assert.Throws<NotSupportedException>(() => repo.Query<Node>().Select(x => new NodeRow { Name = x.Name, Children = x.Children!.Select(c => new NodeRow { Name = c.Name, Children = c.Children!.Select(g => new NodeRow { Name = g.Name }).ToArray() }).ToArray() }).ToArray());
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

            var rows = repo.Query<EventedModel>().Where(x => ids.Contains(x.ID)).OrderBy(x => x.Value).Select(x => new Row { Number = x.Value }).ToArray();
            Assert.Equal([2, 3, 10], rows.Select(r => r.Number).ToArray());

            var groups = await repo.Query<EventedModel>().Where(x => ids.Contains(x.ID)).GroupBy(x => x.Flag).Select(g => new Summary { Flag = g.Key, Rows = g.Count(), Total = g.Sum(x => x.Value) }).ToArrayAsync(cancellationToken: TestContext.Current.CancellationToken);
            Assert.Equal(12, groups.Single(r => r.Flag).Total);
            Assert.Equal(2, groups.Single(r => r.Flag).Rows);
        }

        private sealed class EvenRules(ITransactStoreProvider<Model> next) : BaseTransactStoreRuleProvider<ITransactStoreProvider<Model>, Model>(next)
        {
            public override LambdaExpression? WhereExpression(Graph? graph) => (Expression<Func<Model, bool>>)(x => x.Number % 2 == 0);
        }

        [Fact]
        public async Task SelectBuildsTheModelItself()
        {
            var (repo, models, parents) = MemoryRepo();

            var read = repo.Query<Model>().Where(x => x.ParentID != null).OrderBy(x => x.Number).Select(x => new Model { Number = x.Number, Parent = x.Parent }).ToArray();
            var expected = models.Where(x => x.ParentID is not null).OrderBy(x => x.Number).ToArray();
            Assert.Equal(expected.Select(x => x.Number), read.Select(x => x.Number));
            for (var i = 0; i < read.Length; i++)
                Assert.Equal(parents.Single(p => p.ID == expected[i].ParentID).Name, read[i].Parent!.Name);
            Assert.All(read, x => Assert.Null(x.Name));

            var parent = await repo.Query<Parent>().Where(x => x.ID == parents[0].ID).Select(x => new Parent { Name = x.Name, Children = x.Children }).SingleOrDefaultAsync(cancellationToken: TestContext.Current.CancellationToken);
            Assert.Equal(parents[0].Name, parent!.Name);
            Assert.Equal(models.Where(x => x.ParentID == parents[0].ID).Select(x => x.Number).Order(), parent.Children!.Select(x => x.Number).Order());
        }

        [Entity("TwoWaysOrder")]
        public sealed class TwoWays
        {
            [Identity]
            public Guid ID { get; set; }
            public Guid BillToID { get; set; }
            public Guid ShipToID { get; set; }
            [Relation(nameof(BillToID))]
            public Parent? BillTo { get; set; }
            [Relation(nameof(ShipToID))]
            public Parent? ShipTo { get; set; }
        }

        //one related row joins unless the rows have relations of their own
        [Fact]
        public void SingleRelationsJoin()
        {
            var (repo, models, parents) = MemoryRepo();
            var expression = repo.Query<Model>().Expression;
            var detail = ModelAnalyzer.GetModel(typeof(Model));

            var joined = LinqPlan.Read(new LinqQuery<Model, Model>(expression, null, null, new Graph<Model>(true, x => x.Parent)), detail);
            Assert.Empty(joined.Pieces);
            var relation = Assert.Single(joined.Shape!.ModelRelations!);
            Assert.NotNull(relation.JoinedKey);
            Assert.True(joined.Columns.Count > detail.Members.Count(x => x.StoreName is not null));

            var select = repo.Query<Model>().Select(x => new { x.Number, x.Parent }).Expression;
            Assert.Empty(LinqPlan.Read(new LinqQuery<Model, object>(select, null, null), detail).Pieces);

            //the rows of the relation have one of their own named, so they read by a statement that joins it
            var nested = new Graph<Parent>(x => x.Name, x => x.Children);
            var withChildren = new Graph<Model>(true);
            withChildren.AddChildGraph(nameof(Model.Parent), nested);
            Assert.Single(LinqPlan.Read(new LinqQuery<Model, Model>(expression, null, null, withChildren), detail).Pieces);

            var twoWaysEngine = new MemoryEngine();
            var twoWays = Repo.New();
            twoWays.AddProvider(new TransactStoreProvider<TwoWays>(twoWaysEngine));
            twoWays.AddProvider(new TransactStoreProvider<Parent>(twoWaysEngine));
            var both = LinqPlan.Read(new LinqQuery<TwoWays, TwoWays>(twoWays.Query<TwoWays>().Expression, null, null, new Graph<TwoWays>(true, x => x.BillTo, x => x.ShipTo)), ModelAnalyzer.GetModel(typeof(TwoWays)));
            Assert.Empty(both.Pieces);

            //in memory the related model comes as the graph says and isn't the stored one
            var read = repo.Query<Model>(new Graph<Model>(x => x.Number, x => x.Parent!.Name)).OrderBy(x => x.Number).ToArray();
            Assert.Equal(models.OrderBy(x => x.Number).Select(x => parents.Single(p => p.ID == x.ParentID).Name), read.Select(x => x.Parent!.Name));
            Assert.All(read, x => Assert.Equal(Guid.Empty, x.Parent!.ID));
            var whole = repo.Query<Model>().Select(x => new { x.Number, x.Parent }).ToArray();
            Assert.All(whole, r => Assert.NotEqual(Guid.Empty, r.Parent!.ID));
            Assert.All(whole, r => Assert.Null(r.Parent!.Children));
        }

        //the relations of a graph are read with the rows by statements of their own, and a provider loading models reads its relations itself
        [Fact]
        public void GraphRelationsArePieces()
        {
            var detail = ModelAnalyzer.GetModel(typeof(Parent));
            var engine = new MemoryEngine();
            var (repo, _, _) = MemoryRepo(engine);
            var expression = repo.Query<Parent>().Expression;

            var graph = new Graph<Parent>(true, x => x.Children);
            var plan = LinqPlan.Read(new LinqQuery<Parent, Parent>(expression, null, null, graph), detail);
            var piece = Assert.Single(plan.Pieces);
            Assert.Equal(nameof(Parent.Children), piece.Relation.Name);
            Assert.Null(piece.Where);

            Assert.Empty(LinqPlan.Read(new LinqQuery<Parent, Parent>(expression, null, null, new Graph<Parent>(true)), detail).Pieces);
            Assert.Empty(LinqPlan.Read(new LinqQuery<Parent, Parent>(null, null, null, null, graph, LinqTerminal.Sequence), detail).Pieces);
        }

        private sealed class GraphRecording(ITransactStoreProvider<Model> next) : BaseTransactStoreRuleProvider<ITransactStoreProvider<Model>, Model>(next)
        {
            public Graph? Seen;
            public override void OnQuery(Graph? graph) => Seen = graph;
        }

        private sealed class EvenParents(ITransactStoreProvider<Model> next) : BaseTransactStoreRuleProvider<ITransactStoreProvider<Model>, Model>(next)
        {
            public override LambdaExpression? WhereExpression(Graph? graph) => (Expression<Func<Model, bool>>)(x => x.Number % 2 == 0);
        }

        [Fact]
        public async Task GraphThroughLayers()
        {
            var engine = new MemoryEngine();
            var (_, models, parents) = MemoryRepo(engine);

            //a rule sees the graph
            var recording = new GraphRecording(new TransactStoreProvider<Model>(engine));
            var ruled = Repo.New();
            ruled.AddProvider<Model>(recording);
            _ = ruled.Query<Model>(new Graph<Model>(x => x.Number)).ToArray();
            Assert.True(recording.Seen!.HasMember(nameof(Model.Number)));
            Assert.False(recording.Seen!.HasMember(nameof(Model.Name)));
            _ = ruled.Query<Model>().ToArray();
            Assert.Null(recording.Seen);

            //a layer on the related model filters what the graph loads, so it isn't read in the store
            var related = Repo.New();
            related.AddProvider(new TransactStoreProvider<Parent>(engine));
            related.AddProvider<Model>(new EvenParents(new TransactStoreProvider<Model>(engine)));
            var loaded = await related.Query<Parent>(new Graph<Parent>(true, x => x.Children)).ToArrayAsync(cancellationToken: TestContext.Current.CancellationToken);
            Assert.Equal(parents.Length, loaded.Length);
            foreach (var parent in loaded)
                Assert.Equal(models.Where(m => m.ParentID == parent.ID && m.Number % 2 == 0).Select(m => m.Number).Order(), parent.Children!.Select(m => m.Number).Order());

            //an encrypted member is read when the graph names it
            foreach (var compression in new[] { false, true })
            {
                var layered = Repo.New();
                var root = new TransactStoreProvider<Model>(new MemoryEngine());
                layered.AddProvider<Model>(compression ? new Compressing(root) : new Encrypting(root));
                layered.Create(new Model { ID = Guid.NewGuid(), Number = 1, Note = "secret", Name = "open" });
                Assert.Equal("secret", layered.Query<Model>(new Graph<Model>(x => x.Number, x => x.Note)).ToArray().Single().Note);
                var without = layered.Query<Model>(new Graph<Model>(x => x.Number)).ToArray().Single();
                Assert.Equal(1, without.Number);
                Assert.Null(without.Note);
            }
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
            Assert.Equal(models.Where(x => x.Number % 2 == 0).Select(x => x.Number).Order(), ruled.Query<Model>().Select(x => new Row { Number = x.Number }).ToArray().Select(r => r.Number).Order());
            Assert.Equal(models.Count(x => x.Number % 2 == 0 && x.Flag), (await ruled.Query<Model>().GroupBy(x => x.Flag).Select(g => new Summary { Flag = g.Key, Rows = g.Count() }).ToArrayAsync(cancellationToken: TestContext.Current.CancellationToken)).Single(r => r.Flag).Rows);

            foreach (var compression in new[] { false, true })
            {
                var layered = Repo.New();
                var root = new TransactStoreProvider<Model>(engine);
                layered.AddProvider<Model>(compression ? new Compressing(root) : new Encrypting(root));

                Assert.Equal(models.Length, layered.Query<Model>().Select(x => new Row { Number = x.Number, Name = x.Name }).ToArray().Length);
                _ = Assert.Throws<NotSupportedException>(() => layered.Query<Model>().Select(x => new Row { Name = x.Note }).ToArray());
                _ = Assert.Throws<NotSupportedException>(() => layered.Query<Model>().GroupBy(x => x.Note).Select(g => new Summary { First = g.Key }).ToArray());
                _ = await Assert.ThrowsAsync<NotSupportedException>(() => layered.Query<Model>().GroupBy(x => x.Flag).Select(g => new Summary { First = g.Min(x => x.Note) }).ToArrayAsync(cancellationToken: TestContext.Current.CancellationToken));
            }
        }

        [Fact]
        public async Task QueryableRunsThroughLayersAndEventStores()
        {
            var engine = new MemoryEngine();
            var (_, models, _) = MemoryRepo(engine);

            //a rule's condition applies to the models, the projections, and the values, and its OnGet to the models
            var ruled = Repo.New();
            ruled.AddProvider<Model>(new EvenRules(new TransactStoreProvider<Model>(engine)));
            var even = models.Where(x => x.Number % 2 == 0).ToArray();
            Assert.Equal(even.Select(x => x.Number).Order(), ruled.Query<Model>().ToArray().Select(x => x.Number).Order());
            Assert.Equal(even.Select(x => x.Number * 2).Order(), ruled.Query<Model>().Select(x => new { Twice = x.Number * 2 }).ToArray().Select(r => r.Twice).Order());
            Assert.Equal(even.Sum(x => x.Number), await ruled.Query<Model>().SumAsync(x => x.Number, cancellationToken: TestContext.Current.CancellationToken));
            Assert.Equal(even.Count(x => x.Flag), ruled.Query<Model>().Count(x => x.Flag));

            //an encryption or compression layer refuses reading its properties, and gives the models back plain
            foreach (var compression in new[] { false, true })
            {
                var layered = Repo.New();
                var root = new TransactStoreProvider<Model>(engine);
                layered.AddProvider<Model>(compression ? new Compressing(root) : new Encrypting(root));
                var plain = new Model { ID = Guid.NewGuid(), Number = 100, Note = "secret", Name = "layered" };
                layered.Create(plain);
                Assert.Equal("secret", layered.Query<Model>().Single(x => x.Number == 100).Note);
                Assert.Equal("secret", (await layered.Query<Model>().Where(x => x.Name == "layered").ToArrayAsync(cancellationToken: TestContext.Current.CancellationToken)).Single().Note);
                _ = Assert.Throws<NotSupportedException>(() => layered.Query<Model>().Select(x => new { x.Note }).ToArray());
                _ = Assert.Throws<NotSupportedException>(() => layered.Query<Model>().OrderBy(x => x.Note).ToArray());
                layered.Delete(plain);
            }

            //an event store runs the query over the current state of its models
            var evented = Repo.New();
            evented.AddProvider(new EventStoreAsTransactStoreProvider<EventedModel>(engine));
            var events = new[] { new EventedModel { ID = Guid.NewGuid(), Value = 1, Flag = true }, new EventedModel { ID = Guid.NewGuid(), Value = 2, Flag = true }, new EventedModel { ID = Guid.NewGuid(), Value = 3 } };
            evented.Create<EventedModel>(events);
            events[0].Value = 10;
            evented.Update(events[0]);
            var ids = events.Select(x => x.ID).ToArray();
            var eventedQuery = evented.Query<EventedModel>().Where(x => ids.Contains(x.ID));
            Assert.Equal([2, 3, 10], eventedQuery.OrderBy(x => x.Value).Select(x => new { x.Value }).ToArray().Select(r => r.Value));
            Assert.Equal(12, (await eventedQuery.GroupBy(x => x.Flag).Select(g => new { g.Key, Total = g.Sum(x => x.Value) }).ToArrayAsync(cancellationToken: TestContext.Current.CancellationToken)).Single(r => r.Key).Total);
            Assert.Equal(10, eventedQuery.Max(x => x.Value));
        }

        [Fact]
        public void MembersAreReadFromTheProjection()
        {
            Expression<Func<Model, Model>> set = x => new Model { Number = x.Number + 1, Name = x.Name };
            var members = ProjectionMember.FromSet(set);

            Assert.Equal(["Number", "Name"], members.Select(x => x.MemberDetail.Name));
            Assert.Same(set.Parameters[0], members[0].Parameter);
        }
    }
}
