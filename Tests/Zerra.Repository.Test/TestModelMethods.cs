// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using System;
using System.Linq;

namespace Zerra.Repository.Test
{
    public static class TestModelMethods
    {
        public static void TestSequence<T, U>(ITransactStoreProvider<T> provider, ITransactStoreProvider<U> relationProvider)
            where T : BaseTestTypesModel<U>, new()
            where U : BaseTestRelationsModel, new()
        {
            var model = GetTestTypesModel<T, U>();
            model.KeyA = Guid.NewGuid();
            provider.Persist(new Create<T>(model));

            var modelCheck = (T)provider.Query(new QuerySingle<T>(x => x.KeyA == model.KeyA));
            AssertAreEqual<T, U>(model, modelCheck);

            UpdateModel<T, U>(model);
            provider.Persist(new Update<T>(model));
            modelCheck = (T)provider.Query(new QuerySingle<T>(x => x.KeyA == model.KeyA));
            AssertAreEqual<T, U>(model, modelCheck);

            var relationModel = new U();
            relationModel.RelationAKey = Guid.NewGuid();
            relationProvider.Persist(new Create<U>(relationModel));
            var relationModelCheck = (U)relationProvider.Query(new QuerySingle<U>(x => x.RelationAKey == relationModel.RelationAKey));
            Assert.NotNull(relationModelCheck);

            model.RelationAKey = relationModel.RelationAKey;
            provider.Persist(new Update<T>(model, new Graph<T>(x => x.RelationAKey)));
            modelCheck = (T)provider.Query(new QuerySingle<T>(x => x.KeyA == model.KeyA));
            Assert.Equal(model.RelationAKey, modelCheck.RelationAKey);
            modelCheck = (T)provider.Query(new QuerySingle<T>(x => x.KeyA == model.KeyA, new Graph<T>(x => x.RelationA)));
            Assert.NotNull(modelCheck.RelationA);
            Assert.Equal(model.RelationAKey, modelCheck.RelationA.RelationAKey);

            TestQuery(provider, model, relationModel);

            model.RelationAKey = null;
            provider.Persist(new Update<T>(model, new Graph<T>(x => x.RelationAKey)));
            modelCheck = (T)provider.Query(new QuerySingle<T>(x => x.KeyA == model.KeyA));
            Assert.Equal(model.RelationAKey, modelCheck.RelationAKey);
            modelCheck = (T)provider.Query(new QuerySingle<T>(x => x.KeyA == model.KeyA, new Graph<T>(x => x.RelationA)));
            Assert.Null(model.RelationA);

            provider.Persist(new Delete<T>(model));
            modelCheck = (T)provider.Query(new QuerySingle<T>(x => x.KeyA == model.KeyA));
            Assert.Null(modelCheck);

            relationProvider.Persist(new Delete<U>(relationModel));
            relationModelCheck = (U)relationProvider.Query(new QuerySingle<U>(x => x.RelationAKey == relationModel.RelationAKey));
            Assert.Null(relationModelCheck);
        }

        public static T GetTestTypesModel<T, U>()
            where T : BaseTestTypesModel<U>, new()
            where U : BaseTestRelationsModel, new()
        {
            var model = new T()
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
        public static void UpdateModel<T, U>(T model)
            where T : BaseTestTypesModel<U>, new()
            where U : BaseTestRelationsModel, new()
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
        public static void AssertAreEqual<T, U>(T model1, T model2)
            where T : BaseTestTypesModel<U>, new()
            where U : BaseTestRelationsModel, new()
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
            Assert.Equal(model1.DateTimeThing.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.ff"), model2.DateTimeThing.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.ff"));
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
            Assert.Equal(model1.DateTimeNullableThing.Value.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.ff"), model2.DateTimeNullableThing.Value.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.ff"));
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

        public static void TestQuery<T, U>(ITransactStoreProvider<T> provider, T model, U relationModel)
            where T : BaseTestTypesModel<U>, new()
            where U : BaseTestRelationsModel, new()
        {
            //many
            _ = provider.Query(new QueryMany<T>(x => x.KeyA == model.KeyA));

            //first
            _ = provider.Query(new QueryFirst<T>(x => x.KeyA == model.KeyA));

            //single
            _ = provider.Query(new QuerySingle<T>(x => x.KeyA == model.KeyA));

            //count
            _ = provider.Query(new QueryCount<T>(x => x.KeyA == model.KeyA));

            //any
            _ = provider.Query(new QueryAny<T>(x => x.KeyA == model.KeyA));

            var keyArray = new Guid[] { model.KeyA };

            //array index
            _ = provider.Query(new QuerySingle<T>(x => x.KeyA == keyArray[0]));

            //date
            _ = provider.Query(new QuerySingle<T>(x => x.DateTimeThing > DateTime.Now.AddYears(-1)));

            //date
            _ = provider.Query(new QuerySingle<T>(x => x.DateTimeThing.Year > DateTime.Now.AddYears(-1).Year));

            //time
            _ = provider.Query(new QuerySingle<T>(x => x.TimeSpanThing > TimeSpan.FromMilliseconds(123, 0)));

            //string like
            _ = provider.Query(new QuerySingle<T>(x => x.StringThing.Contains("World")));

            //LINQ contains
            _ = provider.Query(new QuerySingle<T>(x => keyArray.Contains(x.KeyA)));

            //LINQ any
            _ = provider.Query(new QuerySingle<T>(x => x.RelationB.Any(y => y.RelationAKey == relationModel.RelationAKey)));
        }
    }
}
