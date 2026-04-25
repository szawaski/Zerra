// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;

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

            var model = GetTestTypesModel();
            model.KeyA = Guid.NewGuid();
            repo.Create<TestTypesModel>(model);

            var modelCheck = repo.Single<TestTypesModel>(x => x.KeyA == model.KeyA);
            AssertAreEqual(model, modelCheck);

            UpdateModel(model);
            repo.Update<TestTypesModel>(model);
            modelCheck = repo.Single<TestTypesModel>(x => x.KeyA == model.KeyA);
            AssertAreEqual(model, modelCheck);

            var relationAModel = GetTestRelationsModel();
            relationAModel.RelationAKey = Guid.NewGuid();
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

            var relationBModel = GetTestRelationsModel();
            relationBModel.RelationAKey = Guid.NewGuid();
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
        }

        public static async Task TestSequenceAsync<T>() 
            where T : DataContext, new()
        {
            var repo = Repo.New();
            repo.AddProvider(new TransactStoreProvider<T, TestTypesModel>());
            repo.AddProvider(new TransactStoreProvider<T, TestRelationsModel>());

            var model = GetTestTypesModel();
            model.KeyA = Guid.NewGuid();
            await repo.CreateAsync<TestTypesModel>(model);

            var modelCheck = await repo.SingleAsync<TestTypesModel>(x => x.KeyA == model.KeyA);
            AssertAreEqual(model, modelCheck);

            UpdateModel(model);
            await repo.UpdateAsync<TestTypesModel>(model);
            modelCheck = await repo.SingleAsync<TestTypesModel>(x => x.KeyA == model.KeyA);
            AssertAreEqual(model, modelCheck);

            var relationAModel = GetTestRelationsModel();
            relationAModel.RelationAKey = Guid.NewGuid();
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

            var relationBModel = GetTestRelationsModel();
            relationBModel.RelationAKey = Guid.NewGuid();
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
        }

        private static TestTypesModel GetTestTypesModel()
        {
            var model = new TestTypesModel()
            {
                ByteThing = 1,
                Int16Thing = -3,
                Int32Thing = -5,
                Int64Thing = -7,
                SingleThing = -9.1f,
                DoubleThing = -10.2,
                DecimalThing = -11.3m,
                CharThing = 'Z',
                DateTimeThing = DateTime.Now,
                DateTimeOffsetThing = DateTimeOffset.Now.AddDays(1),
                TimeSpanThing = DateTime.Now.TimeOfDay,
                DateOnlyThing = DateOnly.FromDateTime(DateTime.Now),
                TimeOnlyThing = TimeOnly.FromDateTime(DateTime.Now),
                GuidThing = Guid.NewGuid(),

                ByteNullableThing = 11,
                Int16NullableThing = -13,
                Int32NullableThing = -15,
                Int64NullableThing = -17,
                SingleNullableThing = -19.1f,
                DoubleNullableThing = -110.2,
                DecimalNullableThing = -111.3m,
                CharNullableThing = 'X',
                DateTimeNullableThing = DateTime.Now.AddMonths(1),
                DateTimeOffsetNullableThing = DateTimeOffset.Now.AddMonths(1).AddDays(1),
                TimeSpanNullableThing = DateTime.Now.AddHours(1).TimeOfDay,
                DateOnlyNullableThing = DateOnly.FromDateTime(DateTime.Now),
                TimeOnlyNullableThing = TimeOnly.FromDateTime(DateTime.Now),
                GuidNullableThing = Guid.NewGuid(),

                ByteNullableThingNull = null,
                Int16NullableThingNull = null,
                Int32NullableThingNull = null,
                Int64NullableThingNull = null,
                SingleNullableThingNull = null,
                DoubleNullableThingNull = null,
                DecimalNullableThingNull = null,
                CharNullableThingNull = null,
                DateTimeNullableThingNull = null,
                DateTimeOffsetNullableThingNull = null,
                TimeSpanNullableThingNull = null,
                DateOnlyNullableThingNull = null,
                TimeOnlyNullableThingNull = null,
                GuidNullableThingNull = null,

                StringThing = "Hello\r\nWorld!",
                StringThingNull = null,

                BytesThing = [1, 2, 3],
                BytesThingNull = null,
            };
            return model;
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

            var keyArray = new Guid[] { model.KeyA };

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

            var keyArray = new Guid[] { model.KeyA };

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

            Assert.Equal(model1.ByteThing, model2.ByteThing);
            Assert.Equal(model1.Int16Thing, model2.Int16Thing);
            Assert.Equal(model1.Int32Thing, model2.Int32Thing);
            Assert.Equal(model1.Int64Thing, model2.Int64Thing);
            Assert.Equal(model1.SingleThing, model2.SingleThing);
            Assert.Equal(model1.DoubleThing, model2.DoubleThing);
            Assert.Equal(model1.DecimalThing, model2.DecimalThing);
            Assert.Equal(model1.CharThing, model2.CharThing);
            Assert.Equal(model1.DateTimeThing.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.f"), model2.DateTimeThing.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.f"));
            Assert.Equal(model1.DateTimeOffsetThing.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.ffzzz"), model2.DateTimeOffsetThing.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.ffzzz"));
            Assert.Equal((int)model1.TimeSpanThing.TotalMilliseconds, (int)model2.TimeSpanThing.TotalMilliseconds);
            Assert.Equal(model1.DateOnlyThing.ToString("yyyy-MM-dd"), model2.DateOnlyThing.ToString("yyyy-MM-dd"));
            Assert.Equal(model1.TimeOnlyThing.Millisecond, model2.TimeOnlyThing.Millisecond);
            Assert.Equal(model1.GuidThing, model2.GuidThing);

            Assert.Equal(model1.ByteNullableThing, model2.ByteNullableThing);
            Assert.Equal(model1.Int16NullableThing, model2.Int16NullableThing);
            Assert.Equal(model1.Int32NullableThing, model2.Int32NullableThing);
            Assert.Equal(model1.Int64NullableThing, model2.Int64NullableThing);
            Assert.Equal(model1.SingleNullableThing, model2.SingleNullableThing);
            Assert.Equal(model1.DoubleNullableThing, model2.DoubleNullableThing);
            Assert.Equal(model1.DecimalNullableThing, model2.DecimalNullableThing);
            Assert.Equal(model1.CharNullableThing, model2.CharNullableThing);
            Assert.Equal(model1.DateTimeNullableThing.Value.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.f"), model2.DateTimeNullableThing.Value.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.f"));
            Assert.Equal(model1.DateTimeOffsetNullableThing.Value.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.ffzzz"), model2.DateTimeOffsetNullableThing.Value.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.ffzzz"));
            Assert.Equal((int)model1.TimeSpanNullableThing.Value.TotalMilliseconds, (int)model2.TimeSpanNullableThing.Value.TotalMilliseconds);
            Assert.Equal(model1.DateOnlyNullableThing?.ToString("yyyy-MM-dd"), model2.DateOnlyNullableThing?.ToString("yyyy-MM-dd"));
            Assert.Equal(model1.TimeOnlyNullableThing?.Millisecond, model2.TimeOnlyNullableThing?.Millisecond);
            Assert.Equal(model1.GuidNullableThing, model2.GuidNullableThing);

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

        private static TestRelationsModel GetTestRelationsModel()
        {
            var model = new TestRelationsModel()
            {
                SomeValue = "Hello\r\nWorld!"
            };
            return model;
        }
    }
}

