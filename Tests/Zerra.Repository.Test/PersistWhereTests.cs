// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using System.Linq.Expressions;
using Xunit;
using Zerra.Encryption;
using Zerra.Repository.Memory;

namespace Zerra.Repository.Test
{
    //updates and deletes by condition through the layers and the overloads; RepoTest.TestSequence* covers every engine
    public class PersistWhereTests
    {
        [Entity("PersistWhereModel")]
        public sealed class WhereModel
        {
            [Identity]
            public Guid ID { get; set; }
            public int Number { get; set; }
            [StoreProperties(false, 50)]
            public string? Name { get; set; }
            [StoreProperties(false, 50)]
            public string? Note { get; set; }
            public Guid? ParentID { get; set; }
            [Relation(nameof(ParentID))]
            public WhereParent? Parent { get; set; }
        }

        [Entity("PersistWhereParent")]
        public sealed class WhereParent
        {
            [Identity]
            public Guid ID { get; set; }
            [StoreProperties(false, 50)]
            public string? Name { get; set; }
            [Relation(nameof(WhereModel.ParentID))]
            public WhereModel[]? Children { get; set; }
        }

        private static WhereModel[] Models()
        {
            var models = new WhereModel[4];
            for (var i = 0; i < models.Length; i++)
                models[i] = new WhereModel { ID = Guid.NewGuid(), Number = i + 1, Name = $"n{i + 1}", Note = "start" };
            return models;
        }

        private sealed class EvenRules(ITransactStoreProvider<WhereModel> next) : BaseTransactStoreRuleProvider<ITransactStoreProvider<WhereModel>, WhereModel>(next)
        {
            public List<string> Calls { get; } = new();
            public override LambdaExpression? WhereExpression(Graph? graph) => (Expression<Func<WhereModel, bool>>)(x => x.Number % 2 == 0);
            protected override LambdaExpression OnUpdateWhere(LambdaExpression where, object model, Graph? graph)
            {
                Calls.Add($"update {((WhereModel)model).Note}");
                return where;
            }
            protected override LambdaExpression OnDeleteWhere(LambdaExpression where)
            {
                Calls.Add("delete");
                return where;
            }
            protected override IEnumerable OnUpdate(IEnumerable models, Graph? graph)
            {
                Calls.Add("OnUpdate");
                return models;
            }
            protected override ICollection OnDelete(ICollection identities)
            {
                Calls.Add("OnDelete");
                return identities;
            }
            protected override void OnUpdateWhereComplete(LambdaExpression where, object model, Graph? graph, int rowsAffected) => Calls.Add($"updated {rowsAffected}");
            protected override LambdaExpression OnUpdateWhere(LambdaExpression where, LambdaExpression set)
            {
                Calls.Add("update set");
                return where;
            }
            protected override void OnUpdateWhereComplete(LambdaExpression where, LambdaExpression set, int rowsAffected) => Calls.Add($"updated {rowsAffected}");
            protected override void OnDeleteWhereComplete(LambdaExpression where, int rowsAffected) => Calls.Add($"deleted {rowsAffected}");
        }

        [Fact]
        public async Task RuleWhereAppliesAndHooksRun()
        {
            var root = new TransactStoreProvider<WhereModel>(new MemoryEngine());
            var rules = new EvenRules(root);
            var repo = Repo.New();
            repo.AddProvider<WhereModel>(rules);
            repo.Create<WhereModel>(Models());

            //the rule hides the odd numbers, so the condition only reaches the even ones
            Assert.Equal(2, repo.Update<WhereModel>(x => x.Number > 0, new WhereModel { Note = "even" }, new Graph<WhereModel>(x => x.Note)));
            Assert.Equal(["update even", "updated 2"], rules.Calls);
            rules.Calls.Clear();

            Assert.Equal(1, await repo.DeleteAsync<WhereModel>(x => x.Number < 3, TestContext.Current.CancellationToken));
            Assert.Equal(["delete", "deleted 1"], rules.Calls);
            Assert.DoesNotContain("OnUpdate", rules.Calls);
            Assert.DoesNotContain("OnDelete", rules.Calls);

            //the odd numbers the rule hides were never touched
            var raw = Repo.New();
            raw.AddProvider(root);
            Assert.Equal([1, 3, 4], raw.Query<WhereModel>().ToArray().OrderBy(x => x.Number).Select(x => x.Number));
            Assert.Equal(["start", "start", "even"], raw.Query<WhereModel>().ToArray().OrderBy(x => x.Number).Select(x => x.Note));

            //values computed from each model go through the same condition
            rules.Calls.Clear();
            Assert.Equal(1, repo.Update<WhereModel>(x => x.Number > 0, x => new WhereModel { Number = x.Number + 10, Note = x.Name + "!" }));
            Assert.Equal(["update set", "updated 1"], rules.Calls);
            Assert.Equal([1, 3, 14], raw.Query<WhereModel>().ToArray().OrderBy(x => x.Number).Select(x => x.Number));
            Assert.Equal("n4!", raw.Query<WhereModel>().SingleOrDefault(x => x.Number == 14)!.Note);
        }

        private static readonly IEncryptor encryptor = new ZerraEncryptor("where-test", SymmetricAlgorithmType.AES_GCM);

        private sealed class Encrypting(ITransactStoreProvider<WhereModel> next) : BaseTransactStoreEncryptionProvider<ITransactStoreProvider<WhereModel>, WhereModel>(next)
        {
            public override IEncryptor Encryptor => encryptor;
            public override Graph<WhereModel>? Properties => new(x => x.Note);
        }
        private sealed class Compressing(ITransactStoreProvider<WhereModel> next) : BaseTransactStoreCompressionProvider<ITransactStoreProvider<WhereModel>, WhereModel>(next)
        {
            public override Graph<WhereModel>? Properties => new(x => x.Note);
        }

        [Fact]
        public async Task EncryptedAndCompressedLayers()
        {
            foreach (var compression in new[] { false, true })
            {
                var engine = new MemoryEngine();
                var root = new TransactStoreProvider<WhereModel>(engine);
                var layered = Repo.New();
                layered.AddProvider<WhereModel>(compression ? new Compressing(root) : new Encrypting(root));
                var raw = Repo.New();
                raw.AddProvider(root);
                layered.Create<WhereModel>(Models());

                //the set value goes through the layer and reads back plain, while the store holds something else
                Assert.Equal(2, await layered.UpdateAsync<WhereModel>(x => x.Number > 2, new WhereModel { Note = "secret note that is long enough to change" }, new Graph<WhereModel>(x => x.Note), TestContext.Current.CancellationToken));
                Assert.All(layered.Query<WhereModel>().Where(x => x.Number > 2).ToArray(), x => Assert.Equal("secret note that is long enough to change", x.Note));
                Assert.All(raw.Query<WhereModel>().Where(x => x.Number > 2).ToArray(), x => Assert.NotEqual("secret note that is long enough to change", x.Note));

                //values computed in the store can't read or set the transformed property, other members are fine
                Assert.Equal(2, await layered.UpdateAsync<WhereModel>(x => x.Number > 2, x => new WhereModel { Number = x.Number + 10 }, TestContext.Current.CancellationToken));
                Assert.Equal([1, 2, 13, 14], raw.Query<WhereModel>().ToArray().OrderBy(x => x.Number).Select(x => x.Number));
                _ = Assert.Throws<NotSupportedException>(() => layered.Update<WhereModel>(x => x.Number > 2, x => new WhereModel { Note = x.Name }));
                _ = Assert.Throws<NotSupportedException>(() => layered.Update<WhereModel>(x => x.Number > 2, x => new WhereModel { Name = x.Note }));
                _ = await Assert.ThrowsAsync<NotSupportedException>(() => layered.UpdateAsync<WhereModel>(x => x.Note == "start", x => new WhereModel { Number = 1 }, TestContext.Current.CancellationToken));

                //a condition on the transformed property can't match what is stored, so it is refused
                _ = Assert.Throws<NotSupportedException>(() => layered.Update<WhereModel>(x => x.Note == "start", new WhereModel { Note = "x" }, new Graph<WhereModel>(x => x.Note)));
                _ = Assert.Throws<NotSupportedException>(() => layered.Delete<WhereModel>(x => x.Note == "start"));
                _ = await Assert.ThrowsAsync<NotSupportedException>(() => layered.DeleteAsync<WhereModel>(x => x.Note == "start", TestContext.Current.CancellationToken));

                Assert.Equal(1, layered.Delete<WhereModel>(x => x.Number == 1));
                Assert.Equal(3, raw.Query<WhereModel>().Count());
            }
        }

        [Fact]
        public async Task EventStoreRecordsEachChange()
        {
            var repo = Repo.New();
            repo.AddProvider(new EventStoreAsTransactStoreProvider<WhereModel>(new MemoryEngine()));
            var models = Models();
            repo.Create<WhereModel>(models);
            var ids = models.Select(x => x.ID).ToArray();

            Assert.Equal(2, repo.Update<WhereModel>("Bulk", x => ids.Contains(x.ID) && x.Number > 2, new WhereModel { Note = "bulk" }, new Graph<WhereModel>(x => x.Note)));
            //Many on an event store returns every state a model had, Single returns the current one
            Assert.Equal(["start", "start", "bulk", "bulk"], models.Select(m => repo.Query<WhereModel>().SingleOrDefault(x => x.ID == m.ID)!.Note));
            var history = repo.EventMany<WhereModel>(TemporalOrder.Oldest, (ulong?)null, null, null, null, x => x.ID == models[3].ID);
            Assert.Equal(2, history.Count);
            Assert.Equal("Bulk", history.Last().EventName);
            Assert.True(history.Last().GraphChange!.HasMember(nameof(WhereModel.Note)));
            Assert.Equal("n4", history.Last().Model.Name);

            //values computed from each model's current state
            Assert.Equal(2, repo.Update<WhereModel>("Scaled", x => ids.Contains(x.ID) && x.Number > 2, x => new WhereModel { Number = x.Number * 2 }));
            Assert.Equal([1, 2, 6, 8], models.Select(m => repo.Query<WhereModel>().SingleOrDefault(x => x.ID == m.ID)!.Number));
            history = repo.EventMany<WhereModel>(TemporalOrder.Oldest, (ulong?)null, null, null, null, x => x.ID == models[3].ID);
            Assert.Equal(3, history.Count);
            Assert.Equal("Scaled", history.Last().EventName);
            Assert.True(history.Last().GraphChange!.HasMember(nameof(WhereModel.Number)));
            Assert.Equal("bulk", history.Last().Model.Note);

            Assert.Equal(1, await repo.DeleteAsync<WhereModel>(x => ids.Contains(x.ID) && x.Number == 1, TestContext.Current.CancellationToken));
            Assert.Null(repo.Query<WhereModel>().SingleOrDefault(x => x.ID == models[0].ID));
            Assert.Equal(3, ids.Count(id => repo.Query<WhereModel>().Any(x => x.ID == id)));
        }

        [Fact]
        public void MemoryRelationsFollowTheChange()
        {
            var engine = new MemoryEngine();
            var repo = Repo.New();
            repo.AddProvider(new TransactStoreProvider<WhereModel>(engine));
            repo.AddProvider(new TransactStoreProvider<WhereParent>(engine));
            var parents = new[] { new WhereParent { ID = Guid.NewGuid(), Name = "a" }, new WhereParent { ID = Guid.NewGuid(), Name = "b" } };
            repo.Create<WhereParent>(parents);
            var models = Models();
            foreach (var model in models)
                model.ParentID = parents[0].ID;
            repo.Create<WhereModel>(models);

            //a condition through a relation, and the relation reflects the new foreign key afterwards
            Assert.Equal(4, repo.Update<WhereModel>(x => x.Parent!.Name == "a" && x.Number > 0, new WhereModel { ParentID = parents[1].ID }, new Graph<WhereModel>(x => x.ParentID)));
            Assert.Equal(4, repo.Query<WhereModel>().Count(x => x.Parent!.Name == "b"));
            Assert.Equal(4, repo.Query<WhereParent>(new Graph<WhereParent>(x => x.ID, x => x.Children)).Where(x => x.ID == parents[1].ID).SingleOrDefault()!.Children!.Length);
            Assert.Equal(2, repo.Delete<WhereModel>(x => x.Parent!.Name == "b" && x.Number > 2));
            Assert.Equal(2, repo.Query<WhereParent>(new Graph<WhereParent>(x => x.ID, x => x.Children)).Where(x => x.ID == parents[1].ID).SingleOrDefault()!.Children!.Length);

            //values computed from the model, selected through a relation, and never from a relation
            Assert.Equal(2, repo.Update<WhereModel>(x => x.Parent!.Name == "b", x => new WhereModel { Number = x.Number * 100 }));
            Assert.Equal([100, 200], repo.Query<WhereModel>().ToArray().OrderBy(x => x.Number).Select(x => x.Number));
            _ = Assert.Throws<NotSupportedException>(() => repo.Update<WhereModel>(x => x.Number > 0, x => new WhereModel { Name = x.Parent!.Name }));
        }

        private sealed class RecordingProvider : ITransactStoreProvider<WhereModel>
        {
            public Persist? Persist { get; private set; }
            object? ITransactStoreProvider.Query(LinqQuery query) => throw new NotSupportedException();
            Task<object?> ITransactStoreProvider.QueryAsync(LinqQuery query, CancellationToken cancellationToken) => throw new NotSupportedException();
            void ITransactStoreProvider.Persist(Persist persist)
            {
                Persist = persist;
                persist.RowsAffected = 5;
            }
            Task ITransactStoreProvider.PersistAsync(Persist persist, CancellationToken cancellationToken)
            {
                Persist = persist;
                persist.RowsAffected = 6;
                return Task.CompletedTask;
            }
        }

        [Fact]
        public async Task OverloadsPassTheirArguments()
        {
            var provider = new RecordingProvider();
            var repo = Repo.New();
            repo.AddProvider<WhereModel>(provider);
            Expression<Func<WhereModel, bool>> where = x => x.Number > 1;
            var values = new WhereModel { Note = "v" };
            var graph = new Graph<WhereModel>(x => x.Note);
            var source = new object();
            var evt = new PersistEvent(Guid.NewGuid(), "given", source);

            Assert.Equal(5, repo.Delete(where));
            Assert.Equal(PersistOperation.Delete, provider.Persist!.Operation);
            Assert.Same(where, provider.Persist.Where);
            Assert.Null(provider.Persist.Models);
            Assert.Equal("Delete " + typeof(WhereModel).FullName, provider.Persist.Event.Name);

            Assert.Equal(6, await repo.DeleteAsync("named", source, where, TestContext.Current.CancellationToken));
            Assert.Equal("named", provider.Persist!.Event.Name);
            Assert.Same(source, provider.Persist.Event.Source);

            Assert.Equal(5, repo.Delete(evt, where));
            Assert.Same(evt, provider.Persist!.Event);

            Assert.Equal(5, repo.Update(where, values, graph));
            Assert.Equal(PersistOperation.Update, provider.Persist!.Operation);
            Assert.Same(where, provider.Persist.Where);
            Assert.Same(values, Assert.Single(provider.Persist.Models!));
            Assert.Same(graph, provider.Persist.Graph);

            Assert.Equal(6, await repo.UpdateAsync("named", where, values, graph, TestContext.Current.CancellationToken));
            Assert.Equal("named", provider.Persist!.Event.Name);
            Assert.Equal(6, await repo.UpdateAsync(evt, where, values, graph, TestContext.Current.CancellationToken));
            Assert.Same(evt, provider.Persist!.Event);

            Expression<Func<WhereModel, WhereModel>> set = x => new WhereModel { Number = x.Number + 1 };
            Assert.Equal(5, repo.Update(where, set));
            Assert.Equal(PersistOperation.Update, provider.Persist!.Operation);
            Assert.Same(where, provider.Persist.Where);
            Assert.Same(set, provider.Persist.Set);
            Assert.Null(provider.Persist.Models);
            Assert.Null(provider.Persist.Graph);
            Assert.Equal(6, await repo.UpdateAsync("named", source, where, set, TestContext.Current.CancellationToken));
            Assert.Equal("named", provider.Persist!.Event.Name);
            Assert.Same(source, provider.Persist.Event.Source);
            Assert.Equal(5, repo.Update(evt, where, set));
            Assert.Same(evt, provider.Persist!.Event);
            Assert.Equal(6, await repo.UpdateAsync("named", where, set, TestContext.Current.CancellationToken));
            Assert.Equal("named", provider.Persist!.Event.Name);
        }

        [Fact]
        public void UpdateNeedsAGraphWithoutIdentityOrRelation()
        {
            var repo = Repo.New();
            repo.AddProvider(new TransactStoreProvider<WhereModel>(new MemoryEngine()));
            repo.Create<WhereModel>(Models());

            _ = Assert.Throws<ArgumentException>(() => repo.Update<WhereModel>(x => true, new WhereModel(), new Graph<WhereModel>()));
            _ = Assert.Throws<ArgumentException>(() => repo.Update<WhereModel>(x => true, new WhereModel(), new Graph<WhereModel>(x => x.ID)));
            _ = Assert.Throws<ArgumentException>(() => repo.Update<WhereModel>(x => true, new WhereModel(), new Graph<WhereModel>(x => x.Parent)));
            Assert.Equal(4, repo.Update<WhereModel>(x => true, new WhereModel { Note = "all" }, new Graph<WhereModel>(x => x.Note)));

            _ = Assert.Throws<ArgumentException>(() => repo.Update<WhereModel>(x => true, x => new WhereModel { ID = Guid.NewGuid() }));
            _ = Assert.Throws<ArgumentException>(() => repo.Update<WhereModel>(x => true, x => new WhereModel { Parent = null }));
            _ = Assert.Throws<ArgumentException>(() => repo.Update<WhereModel>(x => true, x => new WhereModel()));
            _ = Assert.Throws<ArgumentException>(() => repo.Update<WhereModel>(x => true, x => x));
            Assert.Equal(4, repo.Update<WhereModel>(x => true, x => new WhereModel { Number = x.Number + 1 }));
            Assert.Equal([2, 3, 4, 5], repo.Query<WhereModel>().ToArray().OrderBy(x => x.Number).Select(x => x.Number));
        }
    }
}
