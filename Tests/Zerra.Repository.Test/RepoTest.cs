// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;
using Xunit;
using Zerra.Map;
using Zerra.Repository.Memory;
using Zerra.Repository.Reflection;

namespace Zerra.Repository.Test
{
    public static class RepoTest
    {
        public sealed class TestTypesSelection
        {
            public Guid KeyA { get; set; }
            public int Int32Thing { get; set; }
            public int Doubled { get; set; }
            public string? StringThing { get; set; }
            public bool HasWorld { get; set; }
            public string? RelationAValue { get; set; }
            public int RelationBCount { get; set; }
            public bool RelationBHasModel { get; set; }
        }
        public sealed class TestTypesGrouping
        {
            public bool Flag { get; set; }
            public string? RelationAValue { get; set; }
            public int Rows { get; set; }
            public int Total { get; set; }
            public double Average { get; set; }
            public int Max { get; set; }
            public int Flagged { get; set; }
            public int Distinct { get; set; }
        }

        public static void TestSequenceTransactStore(ITransactStoreEngine engine)
        {
            var repo = Repo.New();
            repo.AddProvider(new TransactStoreProvider<TestTypesModel>(engine));
            repo.AddProvider(new TransactStoreProvider<TestRelationsModel>(engine));

            var model = TestTypesModel.Create();
            repo.Create<TestTypesModel>(model);

            var modelCheck = repo.Query<TestTypesModel>().SingleOrDefault(x => x.KeyA == model.KeyA);
            AssertAreEqual(model, modelCheck);

            UpdateModel(model);
            repo.Update<TestTypesModel>(model);
            modelCheck = repo.Query<TestTypesModel>().SingleOrDefault(x => x.KeyA == model.KeyA);
            AssertAreEqual(model, modelCheck);

            var relationAModel = TestRelationsModel.Create();
            repo.Create<TestRelationsModel>(relationAModel);
            var relationAModelCheck = repo.Query<TestRelationsModel>().SingleOrDefault(x => x.RelationAKey == relationAModel.RelationAKey);
            Assert.NotNull(relationAModelCheck);

            model.RelationAKey = relationAModel.RelationAKey;
            repo.Update<TestTypesModel>(model, new Graph<TestTypesModel>(x => x.RelationAKey));
            modelCheck = repo.Query<TestTypesModel>().SingleOrDefault(x => x.KeyA == model.KeyA);
            Assert.Equal(model.RelationAKey, modelCheck.RelationAKey);
            modelCheck = repo.Query<TestTypesModel>(new Graph<TestTypesModel>(x => x.RelationA)).Where(x => x.KeyA == model.KeyA).SingleOrDefault();
            Assert.NotNull(modelCheck.RelationA);
            Assert.Equal(model.RelationAKey, modelCheck.RelationA.RelationAKey);

            var relationBModel = TestRelationsModel.Create();
            relationBModel.RelationBKey = model.KeyA;
            repo.Create<TestRelationsModel>(relationBModel);
            var relationBModelCheck = repo.Query<TestRelationsModel>().SingleOrDefault(x => x.RelationBKey == model.KeyA);
            Assert.NotNull(relationBModelCheck);

            TestQuery(repo, model, relationBModelCheck);
            TestQueryExpressions(repo, model, relationBModelCheck);

            var repoWithRules = Repo.New();
            repoWithRules.AddProvider(new TestTypesModelRuleProvider(engine));
            repoWithRules.AddProvider(new TestRelationsRule2Provider(engine));

            TestQuery(repoWithRules, model, relationBModelCheck);

            //the model on its own engine and its relations on this one, so everything through a relation crosses engines
            var repoCrossEngine = Repo.New();
            repoCrossEngine.AddProvider(new TransactStoreProvider<TestTypesModel>(new MemoryEngine()));
            repoCrossEngine.AddProvider(new TransactStoreProvider<TestRelationsModel>(engine));
            repoCrossEngine.Create<TestTypesModel>(model);
            TestQuery(repoCrossEngine, model, relationBModelCheck);

            model.RelationAKey = null;
            repo.Update<TestTypesModel>(model, new Graph<TestTypesModel>(x => x.RelationAKey));
            modelCheck = repo.Query<TestTypesModel>().SingleOrDefault(x => x.KeyA == model.KeyA);
            Assert.Equal(model.RelationAKey, modelCheck.RelationAKey);
            modelCheck = repo.Query<TestTypesModel>(new Graph<TestTypesModel>(x => x.RelationA)).Where(x => x.KeyA == model.KeyA).SingleOrDefault();
            Assert.Null(model.RelationA);

            repo.Delete<TestTypesModel>(model);
            modelCheck = repo.Query<TestTypesModel>().SingleOrDefault(x => x.KeyA == model.KeyA);
            Assert.Null(modelCheck);

            repo.Delete<TestRelationsModel>(relationAModel);
            relationAModelCheck = repo.Query<TestRelationsModel>().SingleOrDefault(x => x.RelationAKey == relationAModel.RelationAKey);
            Assert.Null(relationAModelCheck);

            repo.Delete<TestRelationsModel>(relationBModel);
            relationBModelCheck = repo.Query<TestRelationsModel>().SingleOrDefault(x => x.RelationAKey == relationBModel.RelationAKey);
            Assert.Null(relationBModelCheck);

            TestPersistWhere(repo);
            TestPersistWhere(repoWithRules);

            TestRelatedMany(repo);
        }

        /// <summary>
        /// An update or delete by condition changes the rows in the store without loading them, returns how many, and reaches through relations.
        /// </summary>
        private static void TestPersistWhere(IRepo repo)
        {
            var relationX = TestRelationsModel.Create();
            relationX.SomeValue = "where x";
            repo.Create<TestRelationsModel>(relationX);
            var relationY = TestRelationsModel.Create();
            relationY.SomeValue = "where y";
            repo.Create<TestRelationsModel>(relationY);

            var models = new[] { TestTypesModel.Create(), TestTypesModel.Create(), TestTypesModel.Create() };
            for (var i = 0; i < models.Length; i++)
            {
                models[i].Int32Thing = 101 + i;
                models[i].StringThing = "before";
                models[i].RelationAKey = i < 2 ? relationX.RelationAKey : relationY.RelationAKey;
            }
            repo.CreateMany<TestTypesModel>(models);
            var keys = models.Select(x => x.KeyA).ToArray();

            //only the graph's members change on the matching rows
            var updated = repo.Update<TestTypesModel>(x => keys.Contains(x.KeyA) && x.Int32Thing > 101, new TestTypesModel { StringThing = "bulk", Int32Thing = 7 }, new Graph<TestTypesModel>(x => x.StringThing));
            Assert.Equal(2, updated);
            var after = repo.Query<TestTypesModel>().Where(x => keys.Contains(x.KeyA)).ToArray();
            Assert.Equal(3, after.Length);
            Assert.Equal("before", after.Single(x => x.Int32Thing == 101).StringThing);
            Assert.All(after.Where(x => x.Int32Thing > 101), x => Assert.Equal("bulk", x.StringThing));

            //values computed from each row in the store
            Assert.Equal(2, repo.Update<TestTypesModel>(x => keys.Contains(x.KeyA) && x.Int32Thing > 101, x => new TestTypesModel { Int32Thing = x.Int32Thing + 10, StringThing = x.StringThing + "+" }));
            after = repo.Query<TestTypesModel>().Where(x => keys.Contains(x.KeyA)).ToArray();
            Assert.Equal([101, 112, 113], after.Select(x => x.Int32Thing).OrderBy(x => x));
            Assert.Equal(2, after.Count(x => x.StringThing == "bulk+"));

            //nothing matching is zero, not an error
            Assert.Equal(0, repo.Update<TestTypesModel>(x => keys.Contains(x.KeyA) && x.Int32Thing > 1000, new TestTypesModel { StringThing = "none" }, new Graph<TestTypesModel>(x => x.StringThing)));
            Assert.Equal(0, repo.Delete<TestTypesModel>(x => keys.Contains(x.KeyA) && x.Int32Thing > 1000));

            //a condition through a relation
            Assert.Equal(1, repo.Update<TestTypesModel>(x => keys.Contains(x.KeyA) && x.RelationA.SomeValue == "where y", new TestTypesModel { StringThing = "related" }, new Graph<TestTypesModel>(x => x.StringThing)));
            Assert.Equal("related", repo.Query<TestTypesModel>().SingleOrDefault(x => x.KeyA == models[2].KeyA).StringThing);
            Assert.Equal(1, repo.Delete<TestTypesModel>(x => keys.Contains(x.KeyA) && x.RelationA.SomeValue == "where y"));
            Assert.Null(repo.Query<TestTypesModel>().SingleOrDefault(x => x.KeyA == models[2].KeyA));

            Assert.Equal(2, repo.Delete<TestTypesModel>(x => keys.Contains(x.KeyA)));
            Assert.Equal(0, repo.Query<TestTypesModel>().Count(x => keys.Contains(x.KeyA)));
            repo.Delete<TestRelationsModel>(relationX);
            repo.Delete<TestRelationsModel>(relationY);
        }

        private static void TestRelatedMany(IRepo repo)
        {
            //two parents with different related counts loaded together, each gets only its own related models
            var parent1 = TestTypesModel.Create();
            var parent2 = TestTypesModel.Create();
            repo.Create<TestTypesModel>(parent1);
            repo.Create<TestTypesModel>(parent2);

            var related = new TestRelationsModel[] { TestRelationsModel.Create(), TestRelationsModel.Create(), TestRelationsModel.Create() };
            related[0].RelationBKey = parent1.KeyA;
            related[1].RelationBKey = parent1.KeyA;
            related[2].RelationBKey = parent2.KeyA;
            foreach (var item in related)
                repo.Create<TestRelationsModel>(item);

            var keys = new Guid[] { parent1.KeyA, parent2.KeyA };
            var result = repo.Query<TestTypesModel>(new Graph<TestTypesModel>(x => x.KeyA, x => x.RelationB)).Where(x => keys.Contains(x.KeyA)).ToArray();
            AssertRelatedMany(result, parent1, [related[0], related[1]]);
            AssertRelatedMany(result, parent2, [related[2]]);

            foreach (var item in related)
                repo.Delete<TestRelationsModel>(item);
            repo.Delete<TestTypesModel>(parent1);
            repo.Delete<TestTypesModel>(parent2);
        }

        private static async Task TestRelatedManyAsync(IRepo repo)
        {
            //two parents with different related counts loaded together, each gets only its own related models
            var parent1 = TestTypesModel.Create();
            var parent2 = TestTypesModel.Create();
            await repo.CreateAsync<TestTypesModel>(parent1);
            await repo.CreateAsync<TestTypesModel>(parent2);

            var related = new TestRelationsModel[] { TestRelationsModel.Create(), TestRelationsModel.Create(), TestRelationsModel.Create() };
            related[0].RelationBKey = parent1.KeyA;
            related[1].RelationBKey = parent1.KeyA;
            related[2].RelationBKey = parent2.KeyA;
            foreach (var item in related)
                await repo.CreateAsync<TestRelationsModel>(item);

            var keys = new Guid[] { parent1.KeyA, parent2.KeyA };
            var result = await repo.Query<TestTypesModel>(new Graph<TestTypesModel>(x => x.KeyA, x => x.RelationB)).Where(x => keys.Contains(x.KeyA)).ToArrayAsync();
            AssertRelatedMany(result, parent1, [related[0], related[1]]);
            AssertRelatedMany(result, parent2, [related[2]]);

            foreach (var item in related)
                await repo.DeleteAsync<TestRelationsModel>(item);
            await repo.DeleteAsync<TestTypesModel>(parent1);
            await repo.DeleteAsync<TestTypesModel>(parent2);
        }

        private static void AssertRelatedMany(IReadOnlyCollection<TestTypesModel> result, TestTypesModel parent, TestRelationsModel[] expected)
        {
            var match = Assert.Single(result, x => x.KeyA == parent.KeyA);
            Assert.NotNull(match.RelationB);
            Assert.Equal(expected.Length, match.RelationB.Length);
            Assert.All(match.RelationB, x => Assert.Equal(parent.KeyA, x.RelationBKey));
            Assert.Equal(expected.Select(x => x.RelationAKey).Order(), match.RelationB.Select(x => x.RelationAKey).Order());
        }

        public static async Task TestSequenceTransactStoreAsync(ITransactStoreEngine engine)
        {
            var repo = Repo.New();
            repo.AddProvider(new TransactStoreProvider<TestTypesModel>(engine));
            repo.AddProvider(new TransactStoreProvider<TestRelationsModel>(engine));

            var model = TestTypesModel.Create();
            await repo.CreateAsync<TestTypesModel>(model);

            var modelCheck = await repo.Query<TestTypesModel>().SingleOrDefaultAsync(x => x.KeyA == model.KeyA);
            AssertAreEqual(model, modelCheck);

            UpdateModel(model);
            await repo.UpdateAsync<TestTypesModel>(model);
            modelCheck = await repo.Query<TestTypesModel>().SingleOrDefaultAsync(x => x.KeyA == model.KeyA);
            AssertAreEqual(model, modelCheck);

            var relationAModel = TestRelationsModel.Create();
            await repo.CreateAsync<TestRelationsModel>(relationAModel);
            var relationAModelCheck = await repo.Query<TestRelationsModel>().SingleOrDefaultAsync(x => x.RelationAKey == relationAModel.RelationAKey);
            Assert.NotNull(relationAModelCheck);

            model.RelationAKey = relationAModel.RelationAKey;
            await repo.UpdateAsync<TestTypesModel>(model, new Graph<TestTypesModel>(x => x.RelationAKey));
            modelCheck = await repo.Query<TestTypesModel>().SingleOrDefaultAsync(x => x.KeyA == model.KeyA);
            Assert.Equal(model.RelationAKey, modelCheck.RelationAKey);
            modelCheck = await repo.Query<TestTypesModel>(new Graph<TestTypesModel>(x => x.RelationA)).Where(x => x.KeyA == model.KeyA).SingleOrDefaultAsync();
            Assert.NotNull(modelCheck.RelationA);
            Assert.Equal(model.RelationAKey, modelCheck.RelationA.RelationAKey);

            var relationBModel = TestRelationsModel.Create();
            relationBModel.RelationBKey = model.KeyA;
            await repo.CreateAsync<TestRelationsModel>(relationBModel);
            var relationBModelCheck = await repo.Query<TestRelationsModel>().SingleOrDefaultAsync(x => x.RelationBKey == model.KeyA);
            Assert.NotNull(relationBModelCheck);

            await TestQueryAsync(repo, model, relationBModelCheck);

            var repoWithRules = Repo.New();
            repoWithRules.AddProvider(new TestTypesModelRuleProvider(engine));
            repoWithRules.AddProvider(new TestRelationsRule2Provider(engine));

            await TestQueryAsync(repoWithRules, model, relationBModelCheck);

            //the model on its own engine and its relations on this one, so everything through a relation crosses engines
            var repoCrossEngine = Repo.New();
            repoCrossEngine.AddProvider(new TransactStoreProvider<TestTypesModel>(new MemoryEngine()));
            repoCrossEngine.AddProvider(new TransactStoreProvider<TestRelationsModel>(engine));
            await repoCrossEngine.CreateAsync<TestTypesModel>(model);
            await TestQueryAsync(repoCrossEngine, model, relationBModelCheck);

            model.RelationAKey = null;
            await repo.UpdateAsync<TestTypesModel>(model, new Graph<TestTypesModel>(x => x.RelationAKey));
            modelCheck = await repo.Query<TestTypesModel>().SingleOrDefaultAsync(x => x.KeyA == model.KeyA);
            Assert.Equal(model.RelationAKey, modelCheck.RelationAKey);
            modelCheck = await repo.Query<TestTypesModel>(new Graph<TestTypesModel>(x => x.RelationA)).Where(x => x.KeyA == model.KeyA).SingleOrDefaultAsync();
            Assert.Null(model.RelationA);

            await repo.DeleteAsync<TestTypesModel>(model);
            modelCheck = await repo.Query<TestTypesModel>().SingleOrDefaultAsync(x => x.KeyA == model.KeyA);
            Assert.Null(modelCheck);

            await repo.DeleteAsync<TestRelationsModel>(relationAModel);
            relationAModelCheck = await repo.Query<TestRelationsModel>().SingleOrDefaultAsync(x => x.RelationAKey == relationAModel.RelationAKey);
            Assert.Null(relationAModelCheck);

            await repo.DeleteAsync<TestRelationsModel>(relationBModel);
            relationBModelCheck = await repo.Query<TestRelationsModel>().SingleOrDefaultAsync(x => x.RelationAKey == relationBModel.RelationAKey);
            Assert.Null(relationBModelCheck);

            await TestPersistWhereAsync(repo);

            await TestRelatedManyAsync(repo);
        }

        private static async Task TestPersistWhereAsync(IRepo repo)
        {
            var relation = TestRelationsModel.Create();
            relation.SomeValue = "where async";
            await repo.CreateAsync<TestRelationsModel>(relation);

            var models = new[] { TestTypesModel.Create(), TestTypesModel.Create(), TestTypesModel.Create() };
            for (var i = 0; i < models.Length; i++)
            {
                models[i].Int32Thing = 201 + i;
                models[i].StringThing = "before";
                models[i].RelationAKey = relation.RelationAKey;
            }
            await repo.CreateManyAsync<TestTypesModel>(models);
            var keys = models.Select(x => x.KeyA).ToArray();

            Assert.Equal(2, await repo.UpdateAsync<TestTypesModel>(x => keys.Contains(x.KeyA) && x.Int32Thing > 201, new TestTypesModel { StringThing = "bulk" }, new Graph<TestTypesModel>(x => x.StringThing)));
            var after = await repo.Query<TestTypesModel>().Where(x => keys.Contains(x.KeyA)).ToArrayAsync();
            Assert.Equal("before", after.Single(x => x.Int32Thing == 201).StringThing);
            Assert.All(after.Where(x => x.Int32Thing > 201), x => Assert.Equal("bulk", x.StringThing));

            Assert.Equal(2, await repo.UpdateAsync<TestTypesModel>(x => keys.Contains(x.KeyA) && x.Int32Thing > 201, x => new TestTypesModel { Int32Thing = x.Int32Thing + 10 }));
            Assert.Equal([201, 212, 213], (await repo.Query<TestTypesModel>().Where(x => keys.Contains(x.KeyA)).ToArrayAsync()).Select(x => x.Int32Thing).OrderBy(x => x));

            Assert.Equal(0, await repo.DeleteAsync<TestTypesModel>(x => keys.Contains(x.KeyA) && x.Int32Thing > 1000));
            Assert.Equal(3, await repo.DeleteAsync<TestTypesModel>(x => keys.Contains(x.KeyA) && x.RelationA.SomeValue == "where async"));
            Assert.Equal(0, await repo.Query<TestTypesModel>().CountAsync(x => keys.Contains(x.KeyA)));
            await repo.DeleteAsync<TestRelationsModel>(relation);
        }

        /// <summary>
        /// The event store counterpart of <see cref="TestSequenceTransactStore{T}"/>. An event store reads one stream at a time so every
        /// query names the identity, and nothing is overwritten, so the history of the model stays readable through the Event and Temporal calls.
        /// </summary>
        public static void TestSequenceEventStore(IEventStoreEngine engine)
        {
            var repo = Repo.New();
            repo.AddProvider(new EventStoreAsTransactStoreProvider<TestTypesModel>(engine));

            var createdModel = TestTypesModel.Create();
            repo.Create<TestTypesModel>("Created", createdModel);

            var modelCheck = repo.Query<TestTypesModel>().SingleOrDefault(x => x.KeyA == createdModel.KeyA);
            AssertAreEqual(createdModel, modelCheck);

            //a create appends expecting no stream, so the same model cannot be created twice
            _ = Assert.ThrowsAny<Exception>(() => repo.Create<TestTypesModel>("Created", createdModel));

            //the events are read back by date as well as by number, they have to land apart
            Thread.Sleep(20);

            var model = createdModel.Copy();
            UpdateModel(model);
            repo.Update<TestTypesModel>("Updated", model);
            modelCheck = repo.Query<TestTypesModel>().SingleOrDefault(x => x.KeyA == model.KeyA);
            AssertAreEqual(model, modelCheck);

            TestQueryEventStore(repo, createdModel, model);

            var repoWithRules = Repo.New();
            repoWithRules.AddProvider(new TestTypesModelEventRuleProvider(engine));

            TestQueryEventStore(repoWithRules, createdModel, model);

            //an update with a graph records the change for only those members, the rest of the model replays from the events before it
            Thread.Sleep(20);

            var graphChange = model.Copy();
            graphChange.Int32Thing += 1000;
            graphChange.StringThing = "Not In The Graph";
            repo.Update<TestTypesModel>("UpdatedInt32", graphChange, new Graph<TestTypesModel>(x => x.Int32Thing));

            model.Int32Thing = graphChange.Int32Thing;
            modelCheck = repo.Query<TestTypesModel>().SingleOrDefault(x => x.KeyA == model.KeyA);
            AssertAreEqual(model, modelCheck);

            var graphEvent = Assert.Single(repo.EventQuery<TestTypesModel>(TemporalOrder.Oldest, numberFrom: 2).Where(x => x.Model.KeyA == model.KeyA).ToList());
            Assert.Equal("UpdatedInt32", graphEvent.EventName);
            Assert.NotNull(graphEvent.GraphChange);
            Assert.True(graphEvent.GraphChange.HasMember(nameof(TestTypesModel.Int32Thing)));
            Assert.False(graphEvent.GraphChange.HasMember(nameof(TestTypesModel.StringThing)));
            AssertAreEqual(model, graphEvent.Model);

            //a delete terminates the stream, so the model is gone
            repo.Delete<TestTypesModel>("Deleted", model);

            Assert.Null(repo.Query<TestTypesModel>().SingleOrDefault(x => x.KeyA == model.KeyA));
            Assert.Null(repo.Query<TestTypesModel>().FirstOrDefault(x => x.KeyA == model.KeyA));
            Assert.Empty(repo.Query<TestTypesModel>().Where(x => x.KeyA == model.KeyA).ToArray());
            Assert.Equal(0, repo.Query<TestTypesModel>().Count(x => x.KeyA == model.KeyA));
            Assert.False(repo.Query<TestTypesModel>().Any(x => x.KeyA == model.KeyA));

            Assert.Empty(repo.TemporalQuery<TestTypesModel>().Where(x => x.KeyA == model.KeyA).ToList());
            Assert.Equal(0, repo.TemporalQuery<TestTypesModel>().Where(x => x.KeyA == model.KeyA).LongCount());
            Assert.False(repo.TemporalQuery<TestTypesModel>().Where(x => x.KeyA == model.KeyA).Any());

            //the model is gone but its events are still the record of what happened
            var historyAfterDelete = repo.EventQuery<TestTypesModel>(TemporalOrder.Oldest).Where(x => x.Model.KeyA == model.KeyA).ToList();
            Assert.Equal(4, historyAfterDelete.Count);
            Assert.Equal(["Created", "Updated", "UpdatedInt32", "Deleted"], historyAfterDelete.Select(x => x.EventName));
            Assert.Equal([false, false, false, true], historyAfterDelete.Select(x => x.Deleted));
            Assert.Equal(4, repo.EventQuery<TestTypesModel>(TemporalOrder.Oldest).Where(x => x.Model.KeyA == model.KeyA).LongCount());
            Assert.True(repo.EventQuery<TestTypesModel>(TemporalOrder.Oldest).Where(x => x.Model.KeyA == model.KeyA).Any());

            //the newest event is the delete, its model is the state it deleted and it changed nothing
            var deleteEvent = repo.EventQuery<TestTypesModel>().Where(x => x.Model.KeyA == model.KeyA).FirstOrDefault()!;
            Assert.Equal("Deleted", deleteEvent.EventName);
            Assert.True(deleteEvent.Deleted);
            AssertAreEqual(model, deleteEvent.Model);
            Assert.Null(deleteEvent.ModelChange);
            Assert.Null(deleteEvent.GraphChange);
            Assert.Equal("UpdatedInt32", repo.EventQuery<TestTypesModel>().Where(x => x.Model.KeyA == model.KeyA && !x.Deleted).FirstOrDefault()!.EventName);

            //an update or delete by condition finds the streams the condition names and records the change on each
            var byCondition = TestTypesModel.Create();
            repo.Create<TestTypesModel>("Created", byCondition);
            Assert.Equal(1, repo.Update<TestTypesModel>("UpdatedByCondition", x => x.KeyA == byCondition.KeyA, new TestTypesModel { Int32Thing = 77 }, new Graph<TestTypesModel>(x => x.Int32Thing)));
            var changed = repo.Query<TestTypesModel>().SingleOrDefault(x => x.KeyA == byCondition.KeyA);
            Assert.Equal(77, changed.Int32Thing);
            Assert.Equal(byCondition.StringThing, changed.StringThing);
            var conditionEvent = repo.EventQuery<TestTypesModel>().Where(x => x.Model.KeyA == byCondition.KeyA).FirstOrDefault();
            Assert.Equal("UpdatedByCondition", conditionEvent.EventName);
            Assert.True(conditionEvent.GraphChange.HasMember(nameof(TestTypesModel.Int32Thing)));
            Assert.Equal(0, repo.Delete<TestTypesModel>(x => x.KeyA == byCondition.KeyA && x.Int32Thing == 1));
            Assert.Equal(1, repo.Delete<TestTypesModel>("DeletedByCondition", x => x.KeyA == byCondition.KeyA));
            Assert.Null(repo.Query<TestTypesModel>().SingleOrDefault(x => x.KeyA == byCondition.KeyA));
        }

        /// <summary>
        /// The event store counterpart of <see cref="TestSequenceTransactStoreAsync{T}"/>. An event store reads one stream at a time so every
        /// query names the identity, and nothing is overwritten, so the history of the model stays readable through the Event and Temporal calls.
        /// </summary>
        public static async Task TestSequenceEventStoreAsync(IEventStoreEngine engine)
        {
            var repo = Repo.New();
            repo.AddProvider(new EventStoreAsTransactStoreProvider<TestTypesModel>(engine));

            var createdModel = TestTypesModel.Create();
            await repo.CreateAsync<TestTypesModel>("Created", createdModel);

            var modelCheck = await repo.Query<TestTypesModel>().SingleOrDefaultAsync(x => x.KeyA == createdModel.KeyA);
            AssertAreEqual(createdModel, modelCheck);

            //a create appends expecting no stream, so the same model cannot be created twice
            _ = await Assert.ThrowsAnyAsync<Exception>(() => repo.CreateAsync<TestTypesModel>("Created", createdModel));

            //the events are read back by date as well as by number, they have to land apart
            await Task.Delay(20);

            var model = createdModel.Copy();
            UpdateModel(model);
            await repo.UpdateAsync<TestTypesModel>("Updated", model);
            modelCheck = await repo.Query<TestTypesModel>().SingleOrDefaultAsync(x => x.KeyA == model.KeyA);
            AssertAreEqual(model, modelCheck);

            await TestQueryEventStoreAsync(repo, createdModel, model);

            var repoWithRules = Repo.New();
            repoWithRules.AddProvider(new TestTypesModelEventRuleProvider(engine));

            await TestQueryEventStoreAsync(repoWithRules, createdModel, model);

            //an update with a graph records the change for only those members, the rest of the model replays from the events before it
            await Task.Delay(20);

            var graphChange = model.Copy();
            graphChange.Int32Thing += 1000;
            graphChange.StringThing = "Not In The Graph";
            await repo.UpdateAsync<TestTypesModel>("UpdatedInt32", graphChange, new Graph<TestTypesModel>(x => x.Int32Thing));

            model.Int32Thing = graphChange.Int32Thing;
            modelCheck = await repo.Query<TestTypesModel>().SingleOrDefaultAsync(x => x.KeyA == model.KeyA);
            AssertAreEqual(model, modelCheck);

            var graphEvent = Assert.Single(await repo.EventQuery<TestTypesModel>(TemporalOrder.Oldest, numberFrom: 2).Where(x => x.Model.KeyA == model.KeyA).ToListAsync());
            Assert.Equal("UpdatedInt32", graphEvent.EventName);
            Assert.NotNull(graphEvent.GraphChange);
            Assert.True(graphEvent.GraphChange.HasMember(nameof(TestTypesModel.Int32Thing)));
            Assert.False(graphEvent.GraphChange.HasMember(nameof(TestTypesModel.StringThing)));
            AssertAreEqual(model, graphEvent.Model);

            //a delete terminates the stream, so the model is gone
            await repo.DeleteAsync<TestTypesModel>("Deleted", model);

            Assert.Null(await repo.Query<TestTypesModel>().SingleOrDefaultAsync(x => x.KeyA == model.KeyA));
            Assert.Null(await repo.Query<TestTypesModel>().FirstOrDefaultAsync(x => x.KeyA == model.KeyA));
            Assert.Empty(await repo.Query<TestTypesModel>().Where(x => x.KeyA == model.KeyA).ToArrayAsync());
            Assert.Equal(0, await repo.Query<TestTypesModel>().CountAsync(x => x.KeyA == model.KeyA));
            Assert.False(await repo.Query<TestTypesModel>().AnyAsync(x => x.KeyA == model.KeyA));

            Assert.Empty(await repo.TemporalQuery<TestTypesModel>().Where(x => x.KeyA == model.KeyA).ToListAsync());
            Assert.Equal(0, await repo.TemporalQuery<TestTypesModel>().Where(x => x.KeyA == model.KeyA).LongCountAsync());
            Assert.False(await repo.TemporalQuery<TestTypesModel>().Where(x => x.KeyA == model.KeyA).AnyAsync());

            //the model is gone but its events are still the record of what happened
            var historyAfterDelete = await repo.EventQuery<TestTypesModel>(TemporalOrder.Oldest).Where(x => x.Model.KeyA == model.KeyA).ToListAsync();
            Assert.Equal(4, historyAfterDelete.Count);
            Assert.Equal(["Created", "Updated", "UpdatedInt32", "Deleted"], historyAfterDelete.Select(x => x.EventName));
            Assert.Equal([false, false, false, true], historyAfterDelete.Select(x => x.Deleted));
            Assert.Equal(4, await repo.EventQuery<TestTypesModel>(TemporalOrder.Oldest).Where(x => x.Model.KeyA == model.KeyA).LongCountAsync());
            Assert.True(await repo.EventQuery<TestTypesModel>(TemporalOrder.Oldest).Where(x => x.Model.KeyA == model.KeyA).AnyAsync());

            //the newest event is the delete, its model is the state it deleted and it changed nothing
            var deleteEvent = (await repo.EventQuery<TestTypesModel>().Where(x => x.Model.KeyA == model.KeyA).FirstOrDefaultAsync())!;
            Assert.Equal("Deleted", deleteEvent.EventName);
            Assert.True(deleteEvent.Deleted);
            AssertAreEqual(model, deleteEvent.Model);
            Assert.Null(deleteEvent.ModelChange);
            Assert.Null(deleteEvent.GraphChange);
            Assert.Equal("UpdatedInt32", (await repo.EventQuery<TestTypesModel>().Where(x => x.Model.KeyA == model.KeyA && !x.Deleted).FirstOrDefaultAsync())!.EventName);

            //an update or delete by condition finds the streams the condition names and records the change on each
            var byCondition = TestTypesModel.Create();
            await repo.CreateAsync<TestTypesModel>("Created", byCondition);
            Assert.Equal(1, await repo.UpdateAsync<TestTypesModel>("UpdatedByCondition", x => x.KeyA == byCondition.KeyA, new TestTypesModel { Int32Thing = 77 }, new Graph<TestTypesModel>(x => x.Int32Thing)));
            var changed = await repo.Query<TestTypesModel>().SingleOrDefaultAsync(x => x.KeyA == byCondition.KeyA);
            Assert.Equal(77, changed.Int32Thing);
            Assert.Equal(byCondition.StringThing, changed.StringThing);
            var conditionEvent = await repo.EventQuery<TestTypesModel>().Where(x => x.Model.KeyA == byCondition.KeyA).FirstOrDefaultAsync();
            Assert.Equal("UpdatedByCondition", conditionEvent.EventName);
            Assert.Equal(0, await repo.DeleteAsync<TestTypesModel>(x => x.KeyA == byCondition.KeyA && x.Int32Thing == 1));
            Assert.Equal(1, await repo.DeleteAsync<TestTypesModel>("DeletedByCondition", x => x.KeyA == byCondition.KeyA));
            Assert.Null(await repo.Query<TestTypesModel>().SingleOrDefaultAsync(x => x.KeyA == byCondition.KeyA));
        }

        private static void TestQueryEventStore(IRepo repo, TestTypesModel createdModel, TestTypesModel model)
        {
            var missingKey = Guid.NewGuid();

            //single and first replay the whole stream and give the state it ends in
            AssertAreEqual(model, repo.Query<TestTypesModel>().SingleOrDefault(x => x.KeyA == model.KeyA));
            AssertAreEqual(model, repo.Query<TestTypesModel>().FirstOrDefault(x => x.KeyA == model.KeyA));

            //the state after every event, oldest first, is a history query
            var manyResult = repo.TemporalQuery<TestTypesModel>(TemporalOrder.Oldest).Where(x => x.KeyA == model.KeyA).ToList();
            Assert.Equal(2, manyResult.Count);
            AssertAreEqual(createdModel, manyResult.First());
            AssertAreEqual(model, manyResult.Last());

            Assert.Equal(1, repo.Query<TestTypesModel>().Count(x => x.KeyA == model.KeyA));
            Assert.True(repo.Query<TestTypesModel>().Any(x => x.KeyA == model.KeyA));

            //a stream that was never written
            Assert.Null(repo.Query<TestTypesModel>().SingleOrDefault(x => x.KeyA == missingKey));
            Assert.Equal(0, repo.Query<TestTypesModel>().Count(x => x.KeyA == missingKey));
            Assert.False(repo.Query<TestTypesModel>().Any(x => x.KeyA == missingKey));

            //the events themselves, each carrying the change that produced it and the state it produced
            var events = repo.EventQuery<TestTypesModel>(TemporalOrder.Oldest).Where(x => x.Model.KeyA == model.KeyA).ToList();
            Assert.Equal(2, events.Count);
            Assert.Equal(["Created", "Updated"], events.Select(x => x.EventName));
            Assert.Equal([0UL, 1UL], events.Select(x => x.Number));
            Assert.All(events, x => Assert.False(x.Deleted));
            Assert.All(events, x => Assert.NotEqual(Guid.Empty, x.EventID));
            AssertAreEqual(createdModel, events.First().Model);
            AssertAreEqual(model, events.Last().Model);
            AssertAreEqual(model, events.Last().ModelChange);

            var createdDate = events.First().Date;
            var updatedDate = events.Last().Date;
            Assert.True(updatedDate > createdDate, $"{updatedDate:O} is not after {createdDate:O}");

            Assert.Equal(2, repo.EventQuery<TestTypesModel>(TemporalOrder.Oldest).Where(x => x.Model.KeyA == model.KeyA).LongCount());
            Assert.True(repo.EventQuery<TestTypesModel>(TemporalOrder.Oldest).Where(x => x.Model.KeyA == model.KeyA).Any());
            Assert.Equal(0, repo.EventQuery<TestTypesModel>(TemporalOrder.Oldest).Where(x => x.Model.KeyA == missingKey).LongCount());
            Assert.False(repo.EventQuery<TestTypesModel>(TemporalOrder.Oldest).Where(x => x.Model.KeyA == missingKey).Any());

            //bounded by event number
            var eventsFromUpdate = repo.EventQuery<TestTypesModel>(TemporalOrder.Oldest, numberFrom: 1).Where(x => x.Model.KeyA == model.KeyA).ToList();
            AssertAreEqual(model, Assert.Single(eventsFromUpdate).Model);

            //bounded by event date
            var eventsToCreate = repo.EventQuery<TestTypesModel>(TemporalOrder.Oldest, dateTo: createdDate).Where(x => x.Model.KeyA == model.KeyA).ToList();
            AssertAreEqual(createdModel, Assert.Single(eventsToCreate).Model);
            var eventsFromUpdateDate = repo.EventQuery<TestTypesModel>(TemporalOrder.Oldest, dateFrom: updatedDate).Where(x => x.Model.KeyA == model.KeyA).ToList();
            AssertAreEqual(model, Assert.Single(eventsFromUpdateDate).Model);

            //take counts from the end of the stream for newest and skip drops the oldest
            var newestEvent = repo.EventQuery<TestTypesModel>().Where(x => x.Model.KeyA == model.KeyA).Take(1).ToList();
            AssertAreEqual(model, Assert.Single(newestEvent).Model);
            var eventsAfterFirst = repo.EventQuery<TestTypesModel>(TemporalOrder.Oldest).Where(x => x.Model.KeyA == model.KeyA).Skip(1).ToList();
            AssertAreEqual(model, Assert.Single(eventsAfterFirst).Model);

            //the model state at every point in its history
            var states = repo.TemporalQuery<TestTypesModel>(TemporalOrder.Oldest).Where(x => x.KeyA == model.KeyA).ToList();
            Assert.Equal(2, states.Count);
            AssertAreEqual(createdModel, states.First());
            AssertAreEqual(model, states.Last());

            Assert.Equal(2, repo.TemporalQuery<TestTypesModel>(TemporalOrder.Oldest).Where(x => x.KeyA == model.KeyA).LongCount());
            Assert.True(repo.TemporalQuery<TestTypesModel>(TemporalOrder.Oldest).Where(x => x.KeyA == model.KeyA).Any());
            Assert.Equal(0, repo.TemporalQuery<TestTypesModel>(TemporalOrder.Oldest).Where(x => x.KeyA == missingKey).LongCount());
            Assert.False(repo.TemporalQuery<TestTypesModel>(TemporalOrder.Oldest).Where(x => x.KeyA == missingKey).Any());

            //first gives the state the replay ends on
            AssertAreEqual(model, repo.TemporalQuery<TestTypesModel>().Where(x => x.KeyA == model.KeyA).FirstOrDefault());
            Assert.Null(repo.TemporalQuery<TestTypesModel>().Where(x => x.KeyA == missingKey).FirstOrDefault());

            //the event the replay ends on
            var lastEvent = repo.EventQuery<TestTypesModel>().Where(x => x.Model.KeyA == model.KeyA).FirstOrDefault();
            Assert.NotNull(lastEvent);
            Assert.Equal("Updated", lastEvent.EventName);
            Assert.Equal(1UL, lastEvent.Number);
            AssertAreEqual(model, lastEvent.Model);
            Assert.Null(repo.EventQuery<TestTypesModel>().Where(x => x.Model.KeyA == missingKey).FirstOrDefault());

            //single is LINQ's single, so a history of two events is one too many
            _ = Assert.Throws<InvalidOperationException>(() => repo.EventQuery<TestTypesModel>().Where(x => x.Model.KeyA == model.KeyA).SingleOrDefault());
            var singleEvent = repo.EventQuery<TestTypesModel>(numberFrom: 1).Where(x => x.Model.KeyA == model.KeyA).SingleOrDefault();
            Assert.NotNull(singleEvent);
            Assert.Equal("Updated", singleEvent.EventName);
            AssertAreEqual(model, singleEvent.Model);
            Assert.Null(repo.EventQuery<TestTypesModel>().Where(x => x.Model.KeyA == missingKey).SingleOrDefault());

            //states bounded by event date
            var statesToCreate = repo.TemporalQuery<TestTypesModel>(TemporalOrder.Oldest, dateTo: createdDate).Where(x => x.KeyA == model.KeyA).ToList();
            AssertAreEqual(createdModel, Assert.Single(statesToCreate));
            var statesFromUpdate = repo.TemporalQuery<TestTypesModel>(TemporalOrder.Oldest, dateFrom: updatedDate).Where(x => x.KeyA == model.KeyA).ToList();
            AssertAreEqual(model, Assert.Single(statesFromUpdate));

            //states bounded by count from either end of the stream
            var oldestState = repo.TemporalQuery<TestTypesModel>(TemporalOrder.Oldest).Where(x => x.KeyA == model.KeyA).Take(1).ToList();
            AssertAreEqual(createdModel, Assert.Single(oldestState));
            var newestState = repo.TemporalQuery<TestTypesModel>().Where(x => x.KeyA == model.KeyA).Take(1).ToList();
            AssertAreEqual(model, Assert.Single(newestState));
            var statesAfterFirst = repo.TemporalQuery<TestTypesModel>(TemporalOrder.Oldest).Where(x => x.KeyA == model.KeyA).Skip(1).ToList();
            AssertAreEqual(model, Assert.Single(statesAfterFirst));
        }

        private static async Task TestQueryEventStoreAsync(IRepo repo, TestTypesModel createdModel, TestTypesModel model)
        {
            var missingKey = Guid.NewGuid();

            //single and first replay the whole stream and give the state it ends in
            AssertAreEqual(model, await repo.Query<TestTypesModel>().SingleOrDefaultAsync(x => x.KeyA == model.KeyA));
            AssertAreEqual(model, await repo.Query<TestTypesModel>().FirstOrDefaultAsync(x => x.KeyA == model.KeyA));

            //the state after every event, oldest first, is a history query
            var manyResult = (await repo.TemporalQuery<TestTypesModel>(TemporalOrder.Oldest).Where(x => x.KeyA == model.KeyA).ToListAsync()).ToList();
            Assert.Equal(2, manyResult.Count);
            AssertAreEqual(createdModel, manyResult.First());
            AssertAreEqual(model, manyResult.Last());

            Assert.Equal(1, await repo.Query<TestTypesModel>().CountAsync(x => x.KeyA == model.KeyA));
            Assert.True(await repo.Query<TestTypesModel>().AnyAsync(x => x.KeyA == model.KeyA));

            //a stream that was never written
            Assert.Null(await repo.Query<TestTypesModel>().SingleOrDefaultAsync(x => x.KeyA == missingKey));
            Assert.Equal(0, await repo.Query<TestTypesModel>().CountAsync(x => x.KeyA == missingKey));
            Assert.False(await repo.Query<TestTypesModel>().AnyAsync(x => x.KeyA == missingKey));

            //the events themselves, each carrying the change that produced it and the state it produced
            var events = await repo.EventQuery<TestTypesModel>(TemporalOrder.Oldest).Where(x => x.Model.KeyA == model.KeyA).ToListAsync();
            Assert.Equal(2, events.Count);
            Assert.Equal(["Created", "Updated"], events.Select(x => x.EventName));
            Assert.Equal([0UL, 1UL], events.Select(x => x.Number));
            Assert.All(events, x => Assert.False(x.Deleted));
            Assert.All(events, x => Assert.NotEqual(Guid.Empty, x.EventID));
            AssertAreEqual(createdModel, events.First().Model);
            AssertAreEqual(model, events.Last().Model);
            AssertAreEqual(model, events.Last().ModelChange);

            var createdDate = events.First().Date;
            var updatedDate = events.Last().Date;
            Assert.True(updatedDate > createdDate, $"{updatedDate:O} is not after {createdDate:O}");

            Assert.Equal(2, await repo.EventQuery<TestTypesModel>(TemporalOrder.Oldest).Where(x => x.Model.KeyA == model.KeyA).LongCountAsync());
            Assert.True(await repo.EventQuery<TestTypesModel>(TemporalOrder.Oldest).Where(x => x.Model.KeyA == model.KeyA).AnyAsync());
            Assert.Equal(0, await repo.EventQuery<TestTypesModel>(TemporalOrder.Oldest).Where(x => x.Model.KeyA == missingKey).LongCountAsync());
            Assert.False(await repo.EventQuery<TestTypesModel>(TemporalOrder.Oldest).Where(x => x.Model.KeyA == missingKey).AnyAsync());

            //bounded by event number
            var eventsFromUpdate = await repo.EventQuery<TestTypesModel>(TemporalOrder.Oldest, numberFrom: 1).Where(x => x.Model.KeyA == model.KeyA).ToListAsync();
            AssertAreEqual(model, Assert.Single(eventsFromUpdate).Model);

            //bounded by event date
            var eventsToCreate = await repo.EventQuery<TestTypesModel>(TemporalOrder.Oldest, dateTo: createdDate).Where(x => x.Model.KeyA == model.KeyA).ToListAsync();
            AssertAreEqual(createdModel, Assert.Single(eventsToCreate).Model);
            var eventsFromUpdateDate = await repo.EventQuery<TestTypesModel>(TemporalOrder.Oldest, dateFrom: updatedDate).Where(x => x.Model.KeyA == model.KeyA).ToListAsync();
            AssertAreEqual(model, Assert.Single(eventsFromUpdateDate).Model);

            //take counts from the end of the stream for newest and skip drops the oldest
            var newestEvent = await repo.EventQuery<TestTypesModel>().Where(x => x.Model.KeyA == model.KeyA).Take(1).ToListAsync();
            AssertAreEqual(model, Assert.Single(newestEvent).Model);
            var eventsAfterFirst = await repo.EventQuery<TestTypesModel>(TemporalOrder.Oldest).Where(x => x.Model.KeyA == model.KeyA).Skip(1).ToListAsync();
            AssertAreEqual(model, Assert.Single(eventsAfterFirst).Model);

            //the model state at every point in its history
            var states = await repo.TemporalQuery<TestTypesModel>(TemporalOrder.Oldest).Where(x => x.KeyA == model.KeyA).ToListAsync();
            Assert.Equal(2, states.Count);
            AssertAreEqual(createdModel, states.First());
            AssertAreEqual(model, states.Last());

            Assert.Equal(2, await repo.TemporalQuery<TestTypesModel>(TemporalOrder.Oldest).Where(x => x.KeyA == model.KeyA).LongCountAsync());
            Assert.True(await repo.TemporalQuery<TestTypesModel>(TemporalOrder.Oldest).Where(x => x.KeyA == model.KeyA).AnyAsync());
            Assert.Equal(0, await repo.TemporalQuery<TestTypesModel>(TemporalOrder.Oldest).Where(x => x.KeyA == missingKey).LongCountAsync());
            Assert.False(await repo.TemporalQuery<TestTypesModel>(TemporalOrder.Oldest).Where(x => x.KeyA == missingKey).AnyAsync());

            //first gives the state the replay ends on
            AssertAreEqual(model, await repo.TemporalQuery<TestTypesModel>().Where(x => x.KeyA == model.KeyA).FirstOrDefaultAsync());
            Assert.Null(await repo.TemporalQuery<TestTypesModel>().Where(x => x.KeyA == missingKey).FirstOrDefaultAsync());

            //the event the replay ends on
            var lastEvent = await repo.EventQuery<TestTypesModel>().Where(x => x.Model.KeyA == model.KeyA).FirstOrDefaultAsync();
            Assert.NotNull(lastEvent);
            Assert.Equal("Updated", lastEvent.EventName);
            Assert.Equal(1UL, lastEvent.Number);
            AssertAreEqual(model, lastEvent.Model);
            Assert.Null(await repo.EventQuery<TestTypesModel>().Where(x => x.Model.KeyA == missingKey).FirstOrDefaultAsync());

            _ = await Assert.ThrowsAsync<InvalidOperationException>(() => repo.EventQuery<TestTypesModel>().Where(x => x.Model.KeyA == model.KeyA).SingleOrDefaultAsync());
            var singleEvent = await repo.EventQuery<TestTypesModel>(numberFrom: 1).Where(x => x.Model.KeyA == model.KeyA).SingleOrDefaultAsync();
            Assert.NotNull(singleEvent);
            Assert.Equal("Updated", singleEvent.EventName);
            AssertAreEqual(model, singleEvent.Model);
            Assert.Null(await repo.EventQuery<TestTypesModel>().Where(x => x.Model.KeyA == missingKey).SingleOrDefaultAsync());

            //states bounded by event date
            var statesToCreate = await repo.TemporalQuery<TestTypesModel>(TemporalOrder.Oldest, dateTo: createdDate).Where(x => x.KeyA == model.KeyA).ToListAsync();
            AssertAreEqual(createdModel, Assert.Single(statesToCreate));
            var statesFromUpdate = await repo.TemporalQuery<TestTypesModel>(TemporalOrder.Oldest, dateFrom: updatedDate).Where(x => x.KeyA == model.KeyA).ToListAsync();
            AssertAreEqual(model, Assert.Single(statesFromUpdate));

            //states bounded by count from either end of the stream
            var oldestState = await repo.TemporalQuery<TestTypesModel>(TemporalOrder.Oldest).Where(x => x.KeyA == model.KeyA).Take(1).ToListAsync();
            AssertAreEqual(createdModel, Assert.Single(oldestState));
            var newestState = await repo.TemporalQuery<TestTypesModel>().Where(x => x.KeyA == model.KeyA).Take(1).ToListAsync();
            AssertAreEqual(model, Assert.Single(newestState));
            var statesAfterFirst = await repo.TemporalQuery<TestTypesModel>(TemporalOrder.Oldest).Where(x => x.KeyA == model.KeyA).Skip(1).ToListAsync();
            AssertAreEqual(model, Assert.Single(statesAfterFirst));
        }

        /// <summary>
        /// Right after code first generation the data store matches the models, so generating again must find nothing to change.
        /// </summary>
        public static void AssertSchemaMatchesModels(ITransactStoreEngine engine, Type[] modelTypes)
        {
                        var modelDetails = modelTypes.Select(x => ModelAnalyzer.GetModel(x)).ToArray();
            var plan = engine.BuildStoreGenerationPlan(true, true, true, true, modelDetails);
            Assert.True(plan.Plan.Count == 0, $"Schema changes planned right after generation:{Environment.NewLine}{String.Join(Environment.NewLine, plan.Plan)}");
        }

        private static void TestQuery(IRepo repo, TestTypesModel model, TestRelationsModel relationModel)
        {
            //many
            var manyResult = repo.Query<TestTypesModel>().Where(x => x.KeyA == model.KeyA).ToArray();
            Assert.NotNull(manyResult);
            Assert.Single(manyResult);
            AssertAreEqual(model, manyResult.First());

            //first
            var firstResult = repo.Query<TestTypesModel>().FirstOrDefault(x => x.KeyA == model.KeyA);
            AssertAreEqual(model, firstResult);

            //single
            var singleResult = repo.Query<TestTypesModel>().SingleOrDefault(x => x.KeyA == model.KeyA);
            AssertAreEqual(model, singleResult);

            //count
            var countResult = repo.Query<TestTypesModel>().Count(x => x.KeyA == model.KeyA);
            Assert.Equal(1, countResult);

            //any
            var anyResult = repo.Query<TestTypesModel>().Any(x => x.KeyA == model.KeyA);
            Assert.True(anyResult);

            var keyArray = new[] { model.KeyA };

            //array index
            var arrayIndexResult = repo.Query<TestTypesModel>().SingleOrDefault(x => x.KeyA == keyArray[0]);
            AssertAreEqual(model, arrayIndexResult);

            //date
            var dateResult = repo.Query<TestTypesModel>().SingleOrDefault(x => x.DateTimeThing > DateTime.Now.AddYears(-1));
            AssertAreEqual(model, dateResult);

            //date
            var dateYearResult = repo.Query<TestTypesModel>().SingleOrDefault(x => x.DateTimeThing.Year > DateTime.Now.AddYears(-1).Year);
            AssertAreEqual(model, dateYearResult);

            //time
            var timeResult = repo.Query<TestTypesModel>().SingleOrDefault(x => x.TimeSpanThing > TimeSpan.FromMilliseconds(123, 0));
            AssertAreEqual(model, timeResult);

            //bool compared to a value
            var boolValueResult = repo.Query<TestTypesModel>().SingleOrDefault(x => x.KeyA == model.KeyA && x.BooleanThing == model.BooleanThing && x.BooleanNullableThing == model.BooleanNullableThing);
            AssertAreEqual(model, boolValueResult);

            //bool member as a condition, and negated
            var boolMemberResult = model.BooleanThing
                ? repo.Query<TestTypesModel>().SingleOrDefault(x => x.KeyA == model.KeyA && x.BooleanThing && !x.BooleanNullableThing.Value)
                : repo.Query<TestTypesModel>().SingleOrDefault(x => x.KeyA == model.KeyA && !x.BooleanThing && x.BooleanNullableThing.Value);
            AssertAreEqual(model, boolMemberResult);

            //negated bool member excludes the row
            var boolExcludedResult = model.BooleanThing
                ? repo.Query<TestTypesModel>().SingleOrDefault(x => x.KeyA == model.KeyA && !x.BooleanThing)
                : repo.Query<TestTypesModel>().SingleOrDefault(x => x.KeyA == model.KeyA && x.BooleanThing);
            Assert.Null(boolExcludedResult);

            //string like
            var stringLikeResult = repo.Query<TestTypesModel>().SingleOrDefault(x => x.StringThing.Contains("World"));
            AssertAreEqual(model, stringLikeResult);

            //LINQ contains
            var linqContainsResult = repo.Query<TestTypesModel>().SingleOrDefault(x => keyArray.Contains(x.KeyA));
            AssertAreEqual(model, linqContainsResult);

            //LINQ any
            var linqAnyResult = repo.Query<TestTypesModel>().SingleOrDefault(x => x.RelationB.Any(y => y.RelationAKey == relationModel.RelationAKey));
            AssertAreEqual(model, linqAnyResult);

            //Connect 1-1
            var connect11Result = repo.Query<TestTypesModel>(new Graph<TestTypesModel>(x => x.KeyA, x => x.RelationA)).ToArray();
            Assert.NotNull(connect11Result);
            Assert.Contains(connect11Result, m => m.KeyA == model.KeyA);
            var connect11Match = connect11Result.First(m => m.KeyA == model.KeyA);
            Assert.NotNull(connect11Match.RelationA);

            //Connect 1-Many
            var connect1ManyResult = repo.Query<TestTypesModel>(new Graph<TestTypesModel>(x => x.KeyA, x => x.RelationB)).ToArray();
            Assert.NotNull(connect1ManyResult);
            Assert.Contains(connect1ManyResult, m => m.KeyA == model.KeyA);
            var connect1ManyMatch = connect1ManyResult.First(m => m.KeyA == model.KeyA);
            Assert.Contains(connect1ManyMatch.RelationB, r => r.RelationAKey == relationModel.RelationAKey);

            //Connect 1-1 with Where
            var connect11WhereResult = repo.Query<TestTypesModel>(new Graph<TestTypesModel>(x => x.KeyA, x => x.RelationA)).Where(x => x.RelationA.SomeValue.Contains("Hello")).ToArray();
            Assert.NotNull(connect11WhereResult);
            Assert.Contains(connect11WhereResult, m => m.KeyA == model.KeyA);
            var connect11WhereMatch = connect11WhereResult.First(m => m.KeyA == model.KeyA);
            Assert.StartsWith(relationModel.SomeValue, connect11WhereMatch.RelationA.SomeValue);

            //Connect 1-Many with Where
            var connect1ManyWhereResult = repo.Query<TestTypesModel>(new Graph<TestTypesModel>(x => x.KeyA, x => x.RelationB)).Where(x => x.RelationB.Any(y => y.SomeValue.Contains("Hello"))).ToArray();
            Assert.NotNull(connect1ManyWhereResult);
            Assert.Contains(connect1ManyWhereResult, m => m.KeyA == model.KeyA);
            var connect1ManyWhereMatch = connect1ManyWhereResult.First(m => m.KeyA == model.KeyA);
            Assert.Contains(connect1ManyWhereMatch.RelationB, r => r.RelationAKey == relationModel.RelationAKey);

            //select, with members through the relations and an order through one
            var selectResult = repo.Query<TestTypesModel>().Where(x => x.KeyA == model.KeyA).OrderBy(x => x.RelationA.SomeValue).Skip(0).Take(1).Select(x => new TestTypesSelection
            {
                KeyA = x.KeyA,
                Int32Thing = x.Int32Thing,
                Doubled = x.Int32Thing * 2,
                StringThing = x.StringThing,
                HasWorld = x.StringThing.Contains("World"),
                RelationAValue = x.RelationA.SomeValue,
                RelationBCount = x.RelationB.Count(),
                RelationBHasModel = x.RelationB.Any(y => y.RelationAKey == relationModel.RelationAKey)
            }).ToArray();
            var selected = Assert.Single(selectResult);
            Assert.Equal(model.KeyA, selected.KeyA);
            Assert.Equal(model.Int32Thing, selected.Int32Thing);
            Assert.Equal(model.Int32Thing * 2, selected.Doubled);
            Assert.Equal(model.StringThing, selected.StringThing);
            Assert.True(selected.HasWorld);
            Assert.StartsWith(relationModel.SomeValue, selected.RelationAValue);
            Assert.Equal(1, selected.RelationBCount);
            Assert.True(selected.RelationBHasModel);

            //group, keyed by a member with having, order, and paging, then keyed through a relation
            var groupResult = repo.Query<TestTypesModel>().Where(x => x.KeyA == model.KeyA).GroupBy(x => x.BooleanThing).Select(g => new TestTypesGrouping { Flag = g.Key, Rows = g.Count(), Total = g.Sum(x => x.Int32Thing), Average = g.Average(x => x.Int32Thing), Max = g.Max(x => x.Int32Thing), Flagged = g.Count(x => x.BooleanThing), Distinct = g.Select(x => x.StringThing).Distinct().Count() }).Where(r => r.Rows > 0).OrderBy(r => r.Total).Take(1).ToArray();
            var group = Assert.Single(groupResult);
            Assert.Equal(model.BooleanThing, group.Flag);
            Assert.Equal(1, group.Rows);
            Assert.Equal(model.Int32Thing, group.Total);
            Assert.Equal((double)model.Int32Thing, group.Average);
            Assert.Equal(model.Int32Thing, group.Max);
            Assert.Equal(model.BooleanThing ? 1 : 0, group.Flagged);
            Assert.Equal(1, group.Distinct);
            var relationGroupResult = repo.Query<TestTypesModel>().Where(x => x.KeyA == model.KeyA).GroupBy(x => x.RelationA.SomeValue).Select(g => new TestTypesGrouping { RelationAValue = g.Key, Rows = g.Count(), Total = g.Sum(x => x.RelationB.Count()) }).ToArray();
            var relationGroup = Assert.Single(relationGroupResult);
            Assert.StartsWith(relationModel.SomeValue, relationGroup.RelationAValue);
            Assert.Equal(1, relationGroup.Rows);
            Assert.Equal(1, relationGroup.Total);

            //aggregates through the relations
            Assert.Equal(1, repo.Query<TestTypesModel>().Where(x => x.KeyA == model.KeyA).Sum(x => x.RelationB.Count()));
            Assert.Equal(1, repo.Query<TestTypesModel>().Where(x => x.KeyA == model.KeyA).Select(x => x.StringThing).Distinct().Count());
            Assert.Equal([model.BooleanThing], repo.Query<TestTypesModel>().Where(x => x.KeyA == model.KeyA).Select(x => x.BooleanThing).Distinct().ToArray());
            var countByRelation = Assert.Single(repo.Query<TestTypesModel>().Where(x => x.KeyA == model.KeyA).GroupBy(x => x.RelationA.SomeValue).Select(g => new { g.Key, Value = (long)g.Count() }).ToArray().Select(r => (r.Key, r.Value)).ToArray());
            Assert.StartsWith(relationModel.SomeValue, countByRelation.Key);
            Assert.Equal(1, countByRelation.Value);
        }

        private static void TestQueryExpressions(IRepo repo, TestTypesModel model, TestRelationsModel relationModel)
        {
            var key = model.KeyA;
            //the stored values, rounded to the store's precision, decide what each condition should match
            var stored = repo.Query<TestTypesModel>().SingleOrDefault(x => x.KeyA == key);
            Assert.NotNull(stored);

            void AssertFound(Expression<Func<TestTypesModel, bool>> where, bool expected)
            {
                var result = repo.Query<TestTypesModel>().Where(where).ToArray();
                var found = result.Any(m => m.KeyA == key);
                Assert.True(found == expected, $"{where} expected {(expected ? "a match" : "no match")}");
            }
            //the store should match the row the same as running the condition in memory
            void AssertMatches(Expression<Func<TestTypesModel, bool>> where)
            {
                AssertFound(where, where.Compile()(stored));
            }

            //strings
            AssertMatches(x => x.StringThing.StartsWith("Hello"));
            AssertMatches(x => x.StringThing.StartsWith("World"));
            AssertMatches(x => x.StringThing.EndsWith("World!"));
            AssertMatches(x => !x.StringThing.EndsWith("World!"));
            AssertMatches(x => x.StringThing.Contains("o\r\nW"));
            AssertMatches(x => x.StringThing.Contains('W'));
            AssertMatches(x => x.StringLengthThing.Contains("_"));
            AssertMatches(x => x.StringLengthThing.Contains("%"));
            AssertMatches(x => x.StringLengthThing.Contains("[a-z]"));
            AssertMatches(x => x.StringLengthThing.Contains("\\"));
            AssertMatches(x => x.StringLengthThing == "a\\' OR 1=1 -- ");
            AssertMatches(x => x.StringLengthThing.StartsWith("bOUND", StringComparison.OrdinalIgnoreCase));
            AssertMatches(x => x.StringLengthThing.StartsWith(x.StringLengthThing.Substring(0, 3)));
            AssertMatches(x => x.StringLengthThing.StartsWith("Bound") == false);
            AssertMatches(x => x.StringLengthThing.ToUpper() == "BOUNDED");
            AssertMatches(x => x.StringLengthThing.ToLower() == "bounded");
            AssertMatches(x => x.StringLengthThing.Length == 7);
            AssertMatches(x => x.StringThing.Length == 13);
            AssertMatches(x => x.StringLengthThing.Substring(1, 3) == "oun");
            AssertMatches(x => x.StringLengthThing.Substring(2) == "unded");
            AssertMatches(x => x.StringLengthThing.IndexOf("und") == 2);
            AssertMatches(x => x.StringLengthThing.IndexOf('z') == -1);
            AssertMatches(x => x.StringLengthThing.Replace("ound", "OUND") == "BOUNDed");
            AssertMatches(x => x.StringLengthThing + "!" == "Bounded!");
            AssertMatches(x => string.Concat(x.StringLengthThing, "-", x.StringLengthThing) == "Bounded-Bounded");
            AssertMatches(x => x.StringThingNull + "a" == "a");
            AssertMatches(x => string.IsNullOrEmpty(x.StringThingNull));
            AssertMatches(x => string.IsNullOrEmpty(x.StringLengthThing));
            AssertMatches(x => !string.IsNullOrWhiteSpace(x.StringLengthThing));
            AssertMatches(x => x.StringLengthThing.Equals("Bounded"));
            AssertMatches(x => string.Equals(x.StringLengthThing, "bOUNDED", StringComparison.OrdinalIgnoreCase));
            AssertMatches(x => x.StringLengthThing.Trim() == "Bounded");
            string? nullText = null;
            AssertMatches(x => x.StringLengthThing.Contains(nullText ?? "ound"));
            AssertMatches(x => (x.StringThingNull ?? "z") == "z");

            //nullable and coalesce
            AssertMatches(x => x.Int32NullableThing.HasValue);
            AssertMatches(x => x.Int32NullableThingNull.HasValue);
            AssertMatches(x => !x.Int32NullableThingNull.HasValue);
            AssertMatches(x => x.Int32NullableThing.HasValue == true);
            AssertMatches(x => (x.Int32NullableThingNull ?? 7) == 7);
            AssertMatches(x => x.BooleanNullableThingNull ?? true);
            AssertMatches(x => !(x.BooleanNullableThingNull ?? true));
            AssertMatches(x => x.GuidNullableThingNull == default);
            AssertMatches(x => x.KeyA != Guid.NewGuid());

            //date and time parts
            AssertMatches(x => x.DateTimeDefaultPrecisionThing.Year == 2024);
            AssertMatches(x => x.DateTimeDefaultPrecisionThing.Month == 5);
            AssertMatches(x => x.DateTimeDefaultPrecisionThing.Day == 6);
            AssertMatches(x => x.DateTimeDefaultPrecisionThing.Hour == 7);
            AssertMatches(x => x.DateTimeDefaultPrecisionThing.Minute == 8);
            AssertMatches(x => x.DateTimeDefaultPrecisionThing.Second == 9);
            AssertMatches(x => x.DateTimeDefaultPrecisionThing.DayOfYear == 127);
            AssertMatches(x => x.DateTimeDefaultPrecisionThing.DayOfWeek == DayOfWeek.Monday);
            var dayOfWeek = DayOfWeek.Monday;
            AssertMatches(x => x.DateTimeDefaultPrecisionThing.DayOfWeek == dayOfWeek);
            AssertMatches(x => x.DateTimeDefaultPrecisionThing.Date == new DateTime(2024, 5, 6, 0, 0, 0, DateTimeKind.Utc));
            AssertMatches(x => x.DateTimeDefaultPrecisionThing < DateTime.Now.Date);
            //MS SQL's default datetime keeps 1/300 of a second so milliseconds are checked with a precision set
            var dateTime = stored.DateTimeThing;
            AssertMatches(x => x.DateTimeThing.Year == dateTime.Year && x.DateTimeThing.Month == dateTime.Month && x.DateTimeThing.Day == dateTime.Day);
            AssertMatches(x => x.DateTimeThing.Hour == dateTime.Hour && x.DateTimeThing.Minute == dateTime.Minute && x.DateTimeThing.Second == dateTime.Second);
            AssertMatches(x => x.DateTimeThing.Millisecond == dateTime.Millisecond);
            AssertMatches(x => x.DateTimeThing.DayOfYear == dateTime.DayOfYear && x.DateTimeThing.DayOfWeek == dateTime.DayOfWeek);
            AssertMatches(x => x.DateTimeThing.Date == dateTime.Date);
            var dateOnly = stored.DateOnlyThing;
            AssertMatches(x => x.DateOnlyThing.Year == dateOnly.Year && x.DateOnlyThing.Month == dateOnly.Month && x.DateOnlyThing.Day == dateOnly.Day);
            AssertMatches(x => x.DateOnlyThing.DayOfYear == dateOnly.DayOfYear && x.DateOnlyThing.DayOfWeek == dateOnly.DayOfWeek);
            var timeOnly = stored.TimeOnlyThing;
            AssertMatches(x => x.TimeOnlyThing.Hour == timeOnly.Hour && x.TimeOnlyThing.Minute == timeOnly.Minute && x.TimeOnlyThing.Second == timeOnly.Second && x.TimeOnlyThing.Millisecond == timeOnly.Millisecond);
            var timeSpan = stored.TimeSpanThing;
            AssertMatches(x => x.TimeSpanThing.Hours == timeSpan.Hours && x.TimeSpanThing.Minutes == timeSpan.Minutes && x.TimeSpanThing.Seconds == timeSpan.Seconds && x.TimeSpanThing.Milliseconds == timeSpan.Milliseconds);
            var totalHours = timeSpan.TotalHours;
            AssertMatches(x => x.TimeSpanThing.TotalHours > totalHours - 0.0001 && x.TimeSpanThing.TotalHours < totalHours + 0.0001);
            var totalMilliseconds = timeSpan.TotalMilliseconds;
            AssertMatches(x => x.TimeSpanThing.TotalMilliseconds > totalMilliseconds - 1 && x.TimeSpanThing.TotalMilliseconds < totalMilliseconds + 1);
            var offsetYear = stored.DateTimeOffsetThing.Year;
            AssertMatches(x => x.DateTimeOffsetThing.Year == offsetYear);

            //math
            AssertMatches(x => Math.Abs(x.Int32Thing) == 5);
            AssertMatches(x => Math.Floor(x.DoubleThing) == -11);
            AssertMatches(x => Math.Ceiling(x.DoubleThing) == -10);
            AssertMatches(x => Math.Round(x.DecimalThing) == -11m);
            AssertMatches(x => Math.Round(x.DecimalThing, 0) == -11m);
            AssertMatches(x => Math.Round(x.DoubleThing, 1) > -10.21 && Math.Round(x.DoubleThing, 1) < -10.19);
            AssertMatches(x => Math.Pow(x.Int32Thing, 2) == 25);
            AssertMatches(x => Math.Sqrt(x.ByteThing) == 1);
            AssertMatches(x => -(x.Int32Thing + 1) == 4);

            //bitwise
            AssertMatches(x => (x.Int32Thing & 1) == 1);
            AssertMatches(x => (x.Int32Thing | 2) == -5);
            AssertMatches(x => (x.Int32Thing ^ 1) == -6);
            AssertMatches(x => ~x.Int32Thing == 4);
            AssertMatches(x => (x.ByteThing & 2) == 0);

            //values evaluated before the query
            AssertMatches(x => new[] { key }.Contains(x.KeyA));
            AssertMatches(x => new List<Guid> { key }.Contains(x.KeyA));
            var flag = true;
            AssertMatches(x => x.Int32Thing == (flag ? -5 : 0));
            var flagFalse = false;
            AssertMatches(x => x.KeyA == key && !flagFalse);

            //related rows
            var relationKey = relationModel.RelationAKey;
            AssertFound(x => x.RelationB.Count() == 1, true);
            AssertFound(x => x.RelationB.Count() == 2, false);
            AssertFound(x => x.RelationB.LongCount(r => r.SomeValue.StartsWith("Hello")) == 1, true);
            AssertFound(x => x.RelationB.Sum(r => r.RelationAKey) == relationKey, true);
            AssertFound(x => x.RelationB.Min(r => r.RelationAKey) == relationKey, true);
            AssertFound(x => x.RelationB.Max(r => r.RelationAKey) == relationKey, true);
            AssertFound(x => x.RelationB.Average(r => r.RelationAKey) == relationKey, true);
            AssertFound(x => x.RelationB.All(r => r.SomeValue.StartsWith("Hello")), true);
            AssertFound(x => x.RelationB.All(r => r.SomeValue.StartsWith("Nope")), false);
            AssertFound(x => !x.RelationB.All(r => r.SomeValue.StartsWith("Nope")), true);
            AssertFound(x => x.RelationB.Any(r => r.SomeValue.EndsWith("World!")), true);
        }

        private static async Task TestQueryAsync(IRepo repo, TestTypesModel model, TestRelationsModel relationModel)
        {
            //many
            var manyResult = await repo.Query<TestTypesModel>().Where(x => x.KeyA == model.KeyA).ToArrayAsync();
            Assert.NotNull(manyResult);
            Assert.Single(manyResult);
            AssertAreEqual(model, manyResult.First());

            //first
            var firstResult = await repo.Query<TestTypesModel>().FirstOrDefaultAsync(x => x.KeyA == model.KeyA);
            AssertAreEqual(model, firstResult);

            //single
            var singleResult = await repo.Query<TestTypesModel>().SingleOrDefaultAsync(x => x.KeyA == model.KeyA);
            AssertAreEqual(model, singleResult);

            //count
            var countResult = await repo.Query<TestTypesModel>().CountAsync(x => x.KeyA == model.KeyA);
            Assert.Equal(1, countResult);

            //any
            var anyResult = await repo.Query<TestTypesModel>().AnyAsync(x => x.KeyA == model.KeyA);
            Assert.True(anyResult);

            var keyArray = new[] { model.KeyA };

            //array index
            var arrayIndexResult = await repo.Query<TestTypesModel>().SingleOrDefaultAsync(x => x.KeyA == keyArray[0]);
            AssertAreEqual(model, arrayIndexResult);

            //date
            var dateResult = await repo.Query<TestTypesModel>().SingleOrDefaultAsync(x => x.DateTimeThing > DateTime.Now.AddYears(-1));
            AssertAreEqual(model, dateResult);

            //date
            var dateYearResult = await repo.Query<TestTypesModel>().SingleOrDefaultAsync(x => x.DateTimeThing.Year > DateTime.Now.AddYears(-1).Year);
            AssertAreEqual(model, dateYearResult);

            //time
            var timeResult = await repo.Query<TestTypesModel>().SingleOrDefaultAsync(x => x.TimeSpanThing > TimeSpan.FromMilliseconds(123, 0));
            AssertAreEqual(model, timeResult);

            //bool compared to a value
            var boolValueResult = await repo.Query<TestTypesModel>().SingleOrDefaultAsync(x => x.KeyA == model.KeyA && x.BooleanThing == model.BooleanThing && x.BooleanNullableThing == model.BooleanNullableThing);
            AssertAreEqual(model, boolValueResult);

            //bool member as a condition, and negated
            var boolMemberResult = model.BooleanThing
                ? await repo.Query<TestTypesModel>().SingleOrDefaultAsync(x => x.KeyA == model.KeyA && x.BooleanThing && !x.BooleanNullableThing.Value)
                : await repo.Query<TestTypesModel>().SingleOrDefaultAsync(x => x.KeyA == model.KeyA && !x.BooleanThing && x.BooleanNullableThing.Value);
            AssertAreEqual(model, boolMemberResult);

            //negated bool member excludes the row
            var boolExcludedResult = model.BooleanThing
                ? await repo.Query<TestTypesModel>().SingleOrDefaultAsync(x => x.KeyA == model.KeyA && !x.BooleanThing)
                : await repo.Query<TestTypesModel>().SingleOrDefaultAsync(x => x.KeyA == model.KeyA && x.BooleanThing);
            Assert.Null(boolExcludedResult);

            //string like
            var stringLikeResult = await repo.Query<TestTypesModel>().SingleOrDefaultAsync(x => x.StringThing.Contains("World"));
            AssertAreEqual(model, stringLikeResult);

            //LINQ contains
            var linqContainsResult = await repo.Query<TestTypesModel>().SingleOrDefaultAsync(x => keyArray.Contains(x.KeyA));
            AssertAreEqual(model, linqContainsResult);

            //LINQ any
            var linqAnyResult = await repo.Query<TestTypesModel>().SingleOrDefaultAsync(x => x.RelationB.Any(y => y.RelationAKey == relationModel.RelationAKey));
            AssertAreEqual(model, linqAnyResult);

            //Connect 1-1
            var connect11Result = await repo.Query<TestTypesModel>(new Graph<TestTypesModel>(x => x.KeyA, x => x.RelationA)).ToArrayAsync();
            Assert.NotNull(connect11Result);
            Assert.Contains(connect11Result, m => m.KeyA == model.KeyA);
            var connect11Match = connect11Result.First(m => m.KeyA == model.KeyA);
            Assert.NotNull(connect11Match.RelationA);

            //Connect 1-Many
            var connect1ManyResult = await repo.Query<TestTypesModel>(new Graph<TestTypesModel>(x => x.KeyA, x => x.RelationB)).ToArrayAsync();
            Assert.NotNull(connect1ManyResult);
            Assert.Contains(connect1ManyResult, m => m.KeyA == model.KeyA);
            var connect1ManyMatch = connect1ManyResult.First(m => m.KeyA == model.KeyA);
            Assert.Contains(connect1ManyMatch.RelationB, r => r.RelationAKey == relationModel.RelationAKey);

            //Connect 1-1 with Where
            var connect11WhereResult = await repo.Query<TestTypesModel>(new Graph<TestTypesModel>(x => x.KeyA, x => x.RelationA)).Where(x => x.RelationA.SomeValue.Contains("Hello")).ToArrayAsync();
            Assert.NotNull(connect11WhereResult);
            Assert.Contains(connect11WhereResult, m => m.KeyA == model.KeyA);
            var connect11WhereMatch = connect11WhereResult.First(m => m.KeyA == model.KeyA);
            Assert.StartsWith(relationModel.SomeValue, connect11WhereMatch.RelationA.SomeValue);

            //Connect 1-Many with Where
            var connect1ManyWhereResult = await repo.Query<TestTypesModel>(new Graph<TestTypesModel>(x => x.KeyA, x => x.RelationB)).Where(x => x.RelationB.Any(y => y.SomeValue.Contains("Hello"))).ToArrayAsync();
            Assert.NotNull(connect1ManyWhereResult);
            Assert.Contains(connect1ManyWhereResult, m => m.KeyA == model.KeyA);
            var connect1ManyWhereMatch = connect1ManyWhereResult.First(m => m.KeyA == model.KeyA);
            Assert.Contains(connect1ManyWhereMatch.RelationB, r => r.RelationAKey == relationModel.RelationAKey);

            //select, with members through the relations and an order through one
            var selectResult = await repo.Query<TestTypesModel>().Where(x => x.KeyA == model.KeyA).OrderBy(x => x.RelationA.SomeValue).Skip(0).Take(1).Select(x => new TestTypesSelection
            {
                KeyA = x.KeyA,
                Int32Thing = x.Int32Thing,
                Doubled = x.Int32Thing * 2,
                StringThing = x.StringThing,
                HasWorld = x.StringThing.Contains("World"),
                RelationAValue = x.RelationA.SomeValue,
                RelationBCount = x.RelationB.Count(),
                RelationBHasModel = x.RelationB.Any(y => y.RelationAKey == relationModel.RelationAKey)
            }).ToArrayAsync();
            var selected = Assert.Single(selectResult);
            Assert.Equal(model.KeyA, selected.KeyA);
            Assert.Equal(model.Int32Thing, selected.Int32Thing);
            Assert.Equal(model.Int32Thing * 2, selected.Doubled);
            Assert.Equal(model.StringThing, selected.StringThing);
            Assert.True(selected.HasWorld);
            Assert.StartsWith(relationModel.SomeValue, selected.RelationAValue);
            Assert.Equal(1, selected.RelationBCount);
            Assert.True(selected.RelationBHasModel);

            //group, keyed by a member with having, order, and paging, then keyed through a relation
            var groupResult = await repo.Query<TestTypesModel>().Where(x => x.KeyA == model.KeyA).GroupBy(x => x.BooleanThing).Select(g => new TestTypesGrouping { Flag = g.Key, Rows = g.Count(), Total = g.Sum(x => x.Int32Thing), Average = g.Average(x => x.Int32Thing), Max = g.Max(x => x.Int32Thing), Flagged = g.Count(x => x.BooleanThing), Distinct = g.Select(x => x.StringThing).Distinct().Count() }).Where(r => r.Rows > 0).OrderBy(r => r.Total).Take(1).ToArrayAsync();
            var group = Assert.Single(groupResult);
            Assert.Equal(model.BooleanThing, group.Flag);
            Assert.Equal(1, group.Rows);
            Assert.Equal(model.Int32Thing, group.Total);
            Assert.Equal((double)model.Int32Thing, group.Average);
            Assert.Equal(model.Int32Thing, group.Max);
            Assert.Equal(model.BooleanThing ? 1 : 0, group.Flagged);
            Assert.Equal(1, group.Distinct);
            var relationGroupResult = await repo.Query<TestTypesModel>().Where(x => x.KeyA == model.KeyA).GroupBy(x => x.RelationA.SomeValue).Select(g => new TestTypesGrouping { RelationAValue = g.Key, Rows = g.Count(), Total = g.Sum(x => x.RelationB.Count()) }).ToArrayAsync();
            var relationGroup = Assert.Single(relationGroupResult);
            Assert.StartsWith(relationModel.SomeValue, relationGroup.RelationAValue);
            Assert.Equal(1, relationGroup.Rows);
            Assert.Equal(1, relationGroup.Total);

            //aggregates through the relations
            Assert.Equal(1, await repo.Query<TestTypesModel>().Where(x => x.KeyA == model.KeyA).SumAsync(x => x.RelationB.Count()));
            Assert.Equal(1, await repo.Query<TestTypesModel>().Where(x => x.KeyA == model.KeyA).Select(x => x.StringThing).Distinct().CountAsync());
            Assert.Equal([model.BooleanThing], await repo.Query<TestTypesModel>().Where(x => x.KeyA == model.KeyA).Select(x => x.BooleanThing).Distinct().ToArrayAsync());
            var countByRelation = Assert.Single((await repo.Query<TestTypesModel>().Where(x => x.KeyA == model.KeyA).GroupBy(x => x.RelationA.SomeValue).Select(g => new { g.Key, Value = (long)g.Count() }).ToArrayAsync()).Select(r => (r.Key, r.Value)).ToArray());
            Assert.StartsWith(relationModel.SomeValue, countByRelation.Key);
            Assert.Equal(1, countByRelation.Value);
        }

        private static void UpdateModel(TestTypesModel model)
        {
            model.BooleanThing = !model.BooleanThing;
            model.ByteThing++;
            model.Int16Thing++;
            model.Int32Thing++;
            model.Int64Thing++;
            model.SingleThing++;
            model.DoubleThing++;
            model.DecimalThing++;
            model.CharThing = 'Y';
            model.DateTimeThing = DateTime.Now;
            model.DateTimeOffsetThing = DateTimeOffset.Now.AddDays(1);
            model.TimeSpanThing = DateTime.Now.TimeOfDay;
            model.DateOnlyThing = DateOnly.FromDateTime(DateTime.Now.AddDays(1));
            model.TimeOnlyThing = TimeOnly.FromDateTime(DateTime.Now);
            model.GuidThing = Guid.NewGuid();

            model.BooleanNullableThing = !model.BooleanNullableThing;
            model.ByteNullableThing++;
            model.Int16NullableThing++;
            model.Int32NullableThing++;
            model.Int64NullableThing++;
            model.SingleNullableThing++;
            model.DoubleNullableThing++;
            model.DecimalNullableThing++;
            model.CharNullableThing = 'W';
            model.DateTimeNullableThing = DateTime.Now.AddMonths(1);
            model.DateTimeOffsetNullableThing = DateTimeOffset.Now.AddMonths(1).AddDays(1);
            model.TimeSpanNullableThing = DateTime.Now.AddHours(1).TimeOfDay;
            model.DateOnlyNullableThing = DateOnly.FromDateTime(DateTime.Now.AddDays(1));
            model.TimeOnlyNullableThing = TimeOnly.FromDateTime(DateTime.Now);
            model.GuidNullableThing = Guid.NewGuid();
        }

        private static void AssertAreEqual(TestTypesModel model1, TestTypesModel model2)
        {
            Assert.NotNull(model1);
            Assert.NotNull(model2);

            Assert.Equal(model1.BooleanThing, model2.BooleanThing);
            Assert.Equal(model1.ByteThing, model2.ByteThing);
            Assert.Equal(model1.Int16Thing, model2.Int16Thing);
            Assert.Equal(model1.Int32Thing, model2.Int32Thing);
            Assert.Equal(model1.Int64Thing, model2.Int64Thing);
            Assert.Equal(model1.SingleThing, model2.SingleThing);
            Assert.Equal(model1.DoubleThing, model2.DoubleThing);
            Assert.Equal(model1.DecimalThing, model2.DecimalThing);
            Assert.Equal(model1.CharThing, model2.CharThing);
            Assert.Equal(model1.DateTimeThing.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.ff"), model2.DateTimeThing.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.ff"));
            //SQL Server's datetime rounds to 1/300 of a second, a store that drops fractional seconds is off by 456 ms
            Assert.True(Math.Abs((model1.DateTimeDefaultPrecisionThing - model2.DateTimeDefaultPrecisionThing).TotalMilliseconds) < 5, $"{model1.DateTimeDefaultPrecisionThing:O} != {model2.DateTimeDefaultPrecisionThing:O}");
            Assert.Equal(model1.DateTimeOffsetThing.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.ffzzz"), model2.DateTimeOffsetThing.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.ffzzz"));
            Assert.Equal((int)model1.TimeSpanThing.TotalMilliseconds, (int)model2.TimeSpanThing.TotalMilliseconds);
            Assert.Equal(model1.DateOnlyThing.ToString("yyyy-MM-dd"), model2.DateOnlyThing.ToString("yyyy-MM-dd"));
            Assert.Equal(model1.TimeOnlyThing.Millisecond, model2.TimeOnlyThing.Millisecond);
            Assert.Equal(model1.GuidThing, model2.GuidThing);

            Assert.Equal(model1.BooleanNullableThing, model2.BooleanNullableThing);
            Assert.Equal(model1.ByteNullableThing, model2.ByteNullableThing);
            Assert.Equal(model1.Int16NullableThing, model2.Int16NullableThing);
            Assert.Equal(model1.Int32NullableThing, model2.Int32NullableThing);
            Assert.Equal(model1.Int64NullableThing, model2.Int64NullableThing);
            Assert.Equal(model1.SingleNullableThing, model2.SingleNullableThing);
            Assert.Equal(model1.DoubleNullableThing, model2.DoubleNullableThing);
            Assert.Equal(model1.DecimalNullableThing, model2.DecimalNullableThing);
            Assert.Equal(model1.CharNullableThing, model2.CharNullableThing);
            Assert.Equal(model1.DateTimeNullableThing.Value.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.ff"), model2.DateTimeNullableThing.Value.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.ff"));
            Assert.Equal(model1.DateTimeOffsetNullableThing.Value.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.ffzzz"), model2.DateTimeOffsetNullableThing.Value.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.ffzzz"));
            Assert.Equal((int)model1.TimeSpanNullableThing.Value.TotalMilliseconds, (int)model2.TimeSpanNullableThing.Value.TotalMilliseconds);
            Assert.Equal(model1.DateOnlyNullableThing?.ToString("yyyy-MM-dd"), model2.DateOnlyNullableThing?.ToString("yyyy-MM-dd"));
            Assert.Equal(model1.TimeOnlyNullableThing?.Millisecond, model2.TimeOnlyNullableThing?.Millisecond);
            Assert.Equal(model1.GuidNullableThing, model2.GuidNullableThing);

            Assert.Null(model1.BooleanNullableThingNull);
            Assert.Null(model2.BooleanNullableThingNull);
            Assert.Null(model1.ByteNullableThingNull);
            Assert.Null(model1.Int16NullableThingNull);
            Assert.Null(model1.Int32NullableThingNull);
            Assert.Null(model1.Int64NullableThingNull);
            Assert.Null(model1.SingleNullableThingNull);
            Assert.Null(model1.DoubleNullableThingNull);
            Assert.Null(model1.DecimalNullableThingNull);
            Assert.Null(model1.CharNullableThingNull);
            Assert.Null(model1.DateTimeNullableThingNull);
            Assert.Null(model1.DateTimeOffsetNullableThingNull);
            Assert.Null(model1.TimeSpanNullableThingNull);
            Assert.Null(model1.DateOnlyNullableThingNull);
            Assert.Null(model1.TimeOnlyNullableThingNull);
            Assert.Null(model1.GuidNullableThingNull);

            Assert.Equal(model1.StringThing, model2.StringThing);

            Assert.Null(model1.StringThingNull);
            Assert.Null(model2.StringThingNull);
            Assert.Equal(model1.StringLengthThing, model2.StringLengthThing);
            Assert.Null(model2.StringLengthThingNull);

            if (model1.BytesThing is not null)
            {
                Assert.NotNull(model2.BytesThing);
                Assert.Equal(model1.BytesThing.Length, model2.BytesThing.Length);
                for (var i = 0; i < model1.BytesThing.Length; i++)
                    Assert.Equal(model1.BytesThing[i], model2.BytesThing[i]);
            }
            else
            {
                Assert.Null(model2.BytesThing);
            }

            Assert.Null(model1.BytesThingNull);
            Assert.Null(model2.BytesThingNull);
        }
    }
}

