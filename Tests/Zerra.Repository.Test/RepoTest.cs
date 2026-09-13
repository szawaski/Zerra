// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using Zerra.Repository.Reflection;

namespace Zerra.Repository.Test
{
    public static class RepoTest
    {
        public static void TestSequence<T>() 
            where T : DataContext, new()
        {
            var repo = Repo.New();
            repo.AddProvider(new TransactStoreProvider<T, TestTypesModel>());
            repo.AddProvider(new TransactStoreProvider<T, TestRelationsModel>());

            var model = TestTypesModel.Create();
            repo.Create<TestTypesModel>(model);

            var modelCheck = repo.Single<TestTypesModel>(x => x.KeyA == model.KeyA);
            AssertAreEqual(model, modelCheck);

            UpdateModel(model);
            repo.Update<TestTypesModel>(model);
            modelCheck = repo.Single<TestTypesModel>(x => x.KeyA == model.KeyA);
            AssertAreEqual(model, modelCheck);

            var relationAModel = TestRelationsModel.Create();
            repo.Create<TestRelationsModel>(relationAModel);
            var relationAModelCheck = repo.Single<TestRelationsModel>(x => x.RelationAKey == relationAModel.RelationAKey);
            Assert.NotNull(relationAModelCheck);

            model.RelationAKey = relationAModel.RelationAKey;
            repo.Update<TestTypesModel>(model, new Graph<TestTypesModel>(x => x.RelationAKey));
            modelCheck = repo.Single<TestTypesModel>(x => x.KeyA == model.KeyA);
            Assert.Equal(model.RelationAKey, modelCheck.RelationAKey);
            modelCheck = repo.Single<TestTypesModel>(x => x.KeyA == model.KeyA, new Graph<TestTypesModel>(x => x.RelationA));
            Assert.NotNull(modelCheck.RelationA);
            Assert.Equal(model.RelationAKey, modelCheck.RelationA.RelationAKey);

            var relationBModel = TestRelationsModel.Create();
            relationBModel.RelationBKey = model.KeyA;
            repo.Create<TestRelationsModel>(relationBModel);
            var relationBModelCheck = repo.Single<TestRelationsModel>(x => x.RelationBKey == model.KeyA);
            Assert.NotNull(relationBModelCheck);

            TestQuery(repo, model, relationBModelCheck);

            var repoWithRules = Repo.New();
            repoWithRules.AddProvider(new TestTypesModelRuleProvider<T>());
            repoWithRules.AddProvider(new TestRelationsRule2Provider<T>());

            TestQuery(repoWithRules, model, relationBModelCheck);

            model.RelationAKey = null;
            repo.Update<TestTypesModel>(model, new Graph<TestTypesModel>(x => x.RelationAKey));
            modelCheck = repo.Single<TestTypesModel>(x => x.KeyA == model.KeyA);
            Assert.Equal(model.RelationAKey, modelCheck.RelationAKey);
            modelCheck = repo.Single<TestTypesModel>(x => x.KeyA == model.KeyA, new Graph<TestTypesModel>(x => x.RelationA));
            Assert.Null(model.RelationA);

            repo.Delete<TestTypesModel>(model);
            modelCheck = repo.Single<TestTypesModel>(x => x.KeyA == model.KeyA);
            Assert.Null(modelCheck);

            repo.Delete<TestRelationsModel>(relationAModel);
            relationAModelCheck = repo.Single<TestRelationsModel>(x => x.RelationAKey == relationAModel.RelationAKey);
            Assert.Null(relationAModelCheck);

            repo.Delete<TestRelationsModel>(relationBModel);
            relationBModelCheck = repo.Single<TestRelationsModel>(x => x.RelationAKey == relationBModel.RelationAKey);
            Assert.Null(relationBModelCheck);

            TestRelatedMany(repo);
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
            var result = repo.Many<TestTypesModel>(x => keys.Contains(x.KeyA), new Graph<TestTypesModel>(true, x => x.RelationB));
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
            var result = await repo.ManyAsync<TestTypesModel>(x => keys.Contains(x.KeyA), new Graph<TestTypesModel>(true, x => x.RelationB));
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

        public static async Task TestSequenceAsync<T>() 
            where T : DataContext, new()
        {
            var repo = Repo.New();
            repo.AddProvider(new TransactStoreProvider<T, TestTypesModel>());
            repo.AddProvider(new TransactStoreProvider<T, TestRelationsModel>());

            var model = TestTypesModel.Create();
            await repo.CreateAsync<TestTypesModel>(model);

            var modelCheck = await repo.SingleAsync<TestTypesModel>(x => x.KeyA == model.KeyA);
            AssertAreEqual(model, modelCheck);

            UpdateModel(model);
            await repo.UpdateAsync<TestTypesModel>(model);
            modelCheck = await repo.SingleAsync<TestTypesModel>(x => x.KeyA == model.KeyA);
            AssertAreEqual(model, modelCheck);

            var relationAModel = TestRelationsModel.Create();
            await repo.CreateAsync<TestRelationsModel>(relationAModel);
            var relationAModelCheck = await repo.SingleAsync<TestRelationsModel>(x => x.RelationAKey == relationAModel.RelationAKey);
            Assert.NotNull(relationAModelCheck);

            model.RelationAKey = relationAModel.RelationAKey;
            await repo.UpdateAsync<TestTypesModel>(model, new Graph<TestTypesModel>(x => x.RelationAKey));
            modelCheck = await repo.SingleAsync<TestTypesModel>(x => x.KeyA == model.KeyA);
            Assert.Equal(model.RelationAKey, modelCheck.RelationAKey);
            modelCheck = await repo.SingleAsync<TestTypesModel>(x => x.KeyA == model.KeyA, new Graph<TestTypesModel>(x => x.RelationA));
            Assert.NotNull(modelCheck.RelationA);
            Assert.Equal(model.RelationAKey, modelCheck.RelationA.RelationAKey);

            var relationBModel = TestRelationsModel.Create();
            relationBModel.RelationBKey = model.KeyA;
            await repo.CreateAsync<TestRelationsModel>(relationBModel);
            var relationBModelCheck = await repo.SingleAsync<TestRelationsModel>(x => x.RelationBKey == model.KeyA);
            Assert.NotNull(relationBModelCheck);

            await TestQueryAsync(repo, model, relationBModelCheck);

            var repoWithRules = Repo.New();
            repoWithRules.AddProvider(new TestTypesModelRuleProvider<T>());
            repoWithRules.AddProvider(new TestRelationsRule2Provider<T>());

            await TestQueryAsync(repoWithRules, model, relationBModelCheck);

            model.RelationAKey = null;
            await repo.UpdateAsync<TestTypesModel>(model, new Graph<TestTypesModel>(x => x.RelationAKey));
            modelCheck = await repo.SingleAsync<TestTypesModel>(x => x.KeyA == model.KeyA);
            Assert.Equal(model.RelationAKey, modelCheck.RelationAKey);
            modelCheck = await repo.SingleAsync<TestTypesModel>(x => x.KeyA == model.KeyA, new Graph<TestTypesModel>(x => x.RelationA));
            Assert.Null(model.RelationA);

            await repo.DeleteAsync<TestTypesModel>(model);
            modelCheck = await repo.SingleAsync<TestTypesModel>(x => x.KeyA == model.KeyA);
            Assert.Null(modelCheck);

            await repo.DeleteAsync<TestRelationsModel>(relationAModel);
            relationAModelCheck = await repo.SingleAsync<TestRelationsModel>(x => x.RelationAKey == relationAModel.RelationAKey);
            Assert.Null(relationAModelCheck);

            await repo.DeleteAsync<TestRelationsModel>(relationBModel);
            relationBModelCheck = await repo.SingleAsync<TestRelationsModel>(x => x.RelationAKey == relationBModel.RelationAKey);
            Assert.Null(relationBModelCheck);

            await TestRelatedManyAsync(repo);
        }

        /// <summary>
        /// Right after code first generation the data store matches the models, so generating again must find nothing to change.
        /// </summary>
        public static void AssertSchemaMatchesModels<T>(Type[] modelTypes)
            where T : DataContext, new()
        {
            Assert.True(new T().TryGetEngine(out var engine));
            var modelDetails = modelTypes.Select(x => ModelAnalyzer.GetModel(x)).ToArray();
            var plan = engine.BuildStoreGenerationPlan(true, true, true, modelDetails);
            Assert.True(plan.Plan.Count == 0, $"Schema changes planned right after generation:{Environment.NewLine}{String.Join(Environment.NewLine, plan.Plan)}");
        }

        private static void TestQuery(IRepo repo, TestTypesModel model, TestRelationsModel relationModel)
        {
            //many
            var manyResult = repo.Many<TestTypesModel>(x => x.KeyA == model.KeyA);
            Assert.NotNull(manyResult);
            Assert.Single(manyResult);
            AssertAreEqual(model, manyResult.First());

            //first
            var firstResult = repo.First<TestTypesModel>(x => x.KeyA == model.KeyA);
            AssertAreEqual(model, firstResult);

            //single
            var singleResult = repo.Single<TestTypesModel>(x => x.KeyA == model.KeyA);
            AssertAreEqual(model, singleResult);

            //count
            var countResult = repo.Count<TestTypesModel>(x => x.KeyA == model.KeyA);
            Assert.Equal(1, countResult);

            //any
            var anyResult = repo.Any<TestTypesModel>(x => x.KeyA == model.KeyA);
            Assert.True(anyResult);

            var keyArray = new[] { model.KeyA };

            //array index
            var arrayIndexResult = repo.Single<TestTypesModel>(x => x.KeyA == keyArray[0]);
            AssertAreEqual(model, arrayIndexResult);

            //date
            var dateResult = repo.Single<TestTypesModel>(x => x.DateTimeThing > DateTime.Now.AddYears(-1));
            AssertAreEqual(model, dateResult);

            //date
            var dateYearResult = repo.Single<TestTypesModel>(x => x.DateTimeThing.Year > DateTime.Now.AddYears(-1).Year);
            AssertAreEqual(model, dateYearResult);

            //time
            var timeResult = repo.Single<TestTypesModel>(x => x.TimeSpanThing > TimeSpan.FromMilliseconds(123, 0));
            AssertAreEqual(model, timeResult);

            //bool compared to a value
            var boolValueResult = repo.Single<TestTypesModel>(x => x.KeyA == model.KeyA && x.BooleanThing == model.BooleanThing && x.BooleanNullableThing == model.BooleanNullableThing);
            AssertAreEqual(model, boolValueResult);

            //bool member as a condition, and negated
            var boolMemberResult = model.BooleanThing
                ? repo.Single<TestTypesModel>(x => x.KeyA == model.KeyA && x.BooleanThing && !x.BooleanNullableThing.Value)
                : repo.Single<TestTypesModel>(x => x.KeyA == model.KeyA && !x.BooleanThing && x.BooleanNullableThing.Value);
            AssertAreEqual(model, boolMemberResult);

            //negated bool member excludes the row
            var boolExcludedResult = model.BooleanThing
                ? repo.Single<TestTypesModel>(x => x.KeyA == model.KeyA && !x.BooleanThing)
                : repo.Single<TestTypesModel>(x => x.KeyA == model.KeyA && x.BooleanThing);
            Assert.Null(boolExcludedResult);

            //string like
            var stringLikeResult = repo.Single<TestTypesModel>(x => x.StringThing.Contains("World"));
            AssertAreEqual(model, stringLikeResult);

            //LINQ contains
            var linqContainsResult = repo.Single<TestTypesModel>(x => keyArray.Contains(x.KeyA));
            AssertAreEqual(model, linqContainsResult);

            //LINQ any
            var linqAnyResult = repo.Single<TestTypesModel>(x => x.RelationB.Any(y => y.RelationAKey == relationModel.RelationAKey));
            AssertAreEqual(model, linqAnyResult);

            //Connect 1-1
            var connect11Result = repo.Many<TestTypesModel>(new Graph<TestTypesModel>(true, x => x.RelationA));
            Assert.NotNull(connect11Result);
            Assert.Contains(connect11Result, m => m.KeyA == model.KeyA);
            var connect11Match = connect11Result.First(m => m.KeyA == model.KeyA);
            AssertAreEqual(model, connect11Match);

            //Connect 1-Many
            var connect1ManyResult = repo.Many<TestTypesModel>(new Graph<TestTypesModel>(true, x => x.RelationB));
            Assert.NotNull(connect1ManyResult);
            Assert.Contains(connect1ManyResult, m => m.KeyA == model.KeyA);
            var connect1ManyMatch = connect1ManyResult.First(m => m.KeyA == model.KeyA);
            AssertAreEqual(model, connect1ManyMatch);

            //Connect 1-1 with Where
            var connect11WhereResult = repo.Many<TestTypesModel>(x => x.RelationA.SomeValue.Contains("Hello"), new Graph<TestTypesModel>(true, x => x.RelationA));
            Assert.NotNull(connect11WhereResult);
            Assert.Contains(connect11WhereResult, m => m.KeyA == model.KeyA);
            var connect11WhereMatch = connect11WhereResult.First(m => m.KeyA == model.KeyA);
            AssertAreEqual(model, connect11WhereMatch);

            //Connect 1-Many with Where
            var connect1ManyWhereResult = repo.Many<TestTypesModel>(x => x.RelationB.Any(y => y.SomeValue.Contains("Hello")), new Graph<TestTypesModel>(true, x => x.RelationB));
            Assert.NotNull(connect1ManyWhereResult);
            Assert.Contains(connect1ManyWhereResult, m => m.KeyA == model.KeyA);
            var connect1ManyWhereMatch = connect1ManyWhereResult.First(m => m.KeyA == model.KeyA);
            AssertAreEqual(model, connect1ManyWhereMatch);
        }

        private static async Task TestQueryAsync(IRepo repo, TestTypesModel model, TestRelationsModel relationModel)
        {
            //many
            var manyResult = await repo.ManyAsync<TestTypesModel>(x => x.KeyA == model.KeyA);
            Assert.NotNull(manyResult);
            Assert.Single(manyResult);
            AssertAreEqual(model, manyResult.First());

            //first
            var firstResult = await repo.FirstAsync<TestTypesModel>(x => x.KeyA == model.KeyA);
            AssertAreEqual(model, firstResult);

            //single
            var singleResult = await repo.SingleAsync<TestTypesModel>(x => x.KeyA == model.KeyA);
            AssertAreEqual(model, singleResult);

            //count
            var countResult = await repo.CountAsync<TestTypesModel>(x => x.KeyA == model.KeyA);
            Assert.Equal(1, countResult);

            //any
            var anyResult = await repo.AnyAsync<TestTypesModel>(x => x.KeyA == model.KeyA);
            Assert.True(anyResult);

            var keyArray = new[] { model.KeyA };

            //array index
            var arrayIndexResult = await repo.SingleAsync<TestTypesModel>(x => x.KeyA == keyArray[0]);
            AssertAreEqual(model, arrayIndexResult);

            //date
            var dateResult = await repo.SingleAsync<TestTypesModel>(x => x.DateTimeThing > DateTime.Now.AddYears(-1));
            AssertAreEqual(model, dateResult);

            //date
            var dateYearResult = await repo.SingleAsync<TestTypesModel>(x => x.DateTimeThing.Year > DateTime.Now.AddYears(-1).Year);
            AssertAreEqual(model, dateYearResult);

            //time
            var timeResult = await repo.SingleAsync<TestTypesModel>(x => x.TimeSpanThing > TimeSpan.FromMilliseconds(123, 0));
            AssertAreEqual(model, timeResult);

            //bool compared to a value
            var boolValueResult = await repo.SingleAsync<TestTypesModel>(x => x.KeyA == model.KeyA && x.BooleanThing == model.BooleanThing && x.BooleanNullableThing == model.BooleanNullableThing);
            AssertAreEqual(model, boolValueResult);

            //bool member as a condition, and negated
            var boolMemberResult = model.BooleanThing
                ? await repo.SingleAsync<TestTypesModel>(x => x.KeyA == model.KeyA && x.BooleanThing && !x.BooleanNullableThing.Value)
                : await repo.SingleAsync<TestTypesModel>(x => x.KeyA == model.KeyA && !x.BooleanThing && x.BooleanNullableThing.Value);
            AssertAreEqual(model, boolMemberResult);

            //negated bool member excludes the row
            var boolExcludedResult = model.BooleanThing
                ? await repo.SingleAsync<TestTypesModel>(x => x.KeyA == model.KeyA && !x.BooleanThing)
                : await repo.SingleAsync<TestTypesModel>(x => x.KeyA == model.KeyA && x.BooleanThing);
            Assert.Null(boolExcludedResult);

            //string like
            var stringLikeResult = await repo.SingleAsync<TestTypesModel>(x => x.StringThing.Contains("World"));
            AssertAreEqual(model, stringLikeResult);

            //LINQ contains
            var linqContainsResult = await repo.SingleAsync<TestTypesModel>(x => keyArray.Contains(x.KeyA));
            AssertAreEqual(model, linqContainsResult);

            //LINQ any
            var linqAnyResult = await repo.SingleAsync<TestTypesModel>(x => x.RelationB.Any(y => y.RelationAKey == relationModel.RelationAKey));
            AssertAreEqual(model, linqAnyResult);

            //Connect 1-1
            var connect11Result = await repo.ManyAsync<TestTypesModel>(new Graph<TestTypesModel>(true, x => x.RelationA));
            Assert.NotNull(connect11Result);
            Assert.Contains(connect11Result, m => m.KeyA == model.KeyA);
            var connect11Match = connect11Result.First(m => m.KeyA == model.KeyA);
            AssertAreEqual(model, connect11Match);

            //Connect 1-Many
            var connect1ManyResult = await repo.ManyAsync<TestTypesModel>(new Graph<TestTypesModel>(true, x => x.RelationB));
            Assert.NotNull(connect1ManyResult);
            Assert.Contains(connect1ManyResult, m => m.KeyA == model.KeyA);
            var connect1ManyMatch = connect1ManyResult.First(m => m.KeyA == model.KeyA);
            AssertAreEqual(model, connect1ManyMatch);

            //Connect 1-1 with Where
            var connect11WhereResult = await repo.ManyAsync<TestTypesModel>(x => x.RelationA.SomeValue.Contains("Hello"), new Graph<TestTypesModel>(true, x => x.RelationA));
            Assert.NotNull(connect11WhereResult);
            Assert.Contains(connect11WhereResult, m => m.KeyA == model.KeyA);
            var connect11WhereMatch = connect11WhereResult.First(m => m.KeyA == model.KeyA);
            AssertAreEqual(model, connect11WhereMatch);

            //Connect 1-Many with Where
            var connect1ManyWhereResult = await repo.ManyAsync<TestTypesModel>(x => x.RelationB.Any(y => y.SomeValue.Contains("Hello")), new Graph<TestTypesModel>(true, x => x.RelationB));
            Assert.NotNull(connect1ManyWhereResult);
            Assert.Contains(connect1ManyWhereResult, m => m.KeyA == model.KeyA);
            var connect1ManyWhereMatch = connect1ManyWhereResult.First(m => m.KeyA == model.KeyA);
            AssertAreEqual(model, connect1ManyWhereMatch);
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

