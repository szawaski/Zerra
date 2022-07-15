// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.VisualStudio.TestTools.UnitTesting;
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
            Assert.IsNotNull(relationModelCheck);

            model.RelationAKey = relationModel.RelationAKey;
            provider.Persist(new Update<T>(model, new Graph<T>(x => x.RelationAKey)));
            modelCheck = (T)provider.Query(new QuerySingle<T>(x => x.KeyA == model.KeyA));
            Assert.AreEqual(model.RelationAKey, modelCheck.RelationAKey);
            modelCheck = (T)provider.Query(new QuerySingle<T>(x => x.KeyA == model.KeyA, new Graph<T>(x => x.RelationA)));
            Assert.IsNotNull(modelCheck.RelationA);
            Assert.AreEqual(model.RelationAKey, modelCheck.RelationA.RelationAKey);

            TestQuery(provider, model, relationModel);

            model.RelationAKey = null;
            provider.Persist(new Update<T>(model, new Graph<T>(x => x.RelationAKey)));
            modelCheck = (T)provider.Query(new QuerySingle<T>(x => x.KeyA == model.KeyA));
            Assert.AreEqual(model.RelationAKey, modelCheck.RelationAKey);
            modelCheck = (T)provider.Query(new QuerySingle<T>(x => x.KeyA == model.KeyA, new Graph<T>(x => x.RelationA)));
            Assert.IsNull(model.RelationA);

            provider.Persist(new Delete<T>(model));
            modelCheck = (T)provider.Query(new QuerySingle<T>(x => x.KeyA == model.KeyA));
            Assert.IsNull(modelCheck);

            relationProvider.Persist(new Delete<U>(relationModel));
            relationModelCheck = (U)relationProvider.Query(new QuerySingle<U>(x => x.RelationAKey == relationModel.RelationAKey));
            Assert.IsNull(relationModelCheck);
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
                GuidNullableThingNull = null,

                StringThing = "Hello\r\nWorld!",
                StringThingNull = null,

                BytesThing = new byte[] { 1, 2, 3 },
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
            model.GuidNullableThing = Guid.NewGuid();
        }
        public static void AssertAreEqual<T, U>(T model1, T model2)
            where T : BaseTestTypesModel<U>, new()
            where U : BaseTestRelationsModel, new()
        {
            Assert.IsNotNull(model1);
            Assert.IsNotNull(model2);

            Assert.AreEqual(model1.ByteThing, model2.ByteThing);
            Assert.AreEqual(model1.Int16Thing, model2.Int16Thing);
            Assert.AreEqual(model1.Int32Thing, model2.Int32Thing);
            Assert.AreEqual(model1.Int64Thing, model2.Int64Thing);
            Assert.AreEqual(model1.SingleThing, model2.SingleThing);
            Assert.AreEqual(model1.DoubleThing, model2.DoubleThing);
            Assert.AreEqual(model1.DecimalThing, model2.DecimalThing);
            Assert.AreEqual(model1.CharThing, model2.CharThing);
            Assert.AreEqual(model1.DateTimeThing.ToUniversalTime().ToString("yyyyMMddHHmmss.f"), model2.DateTimeThing.ToUniversalTime().ToString("yyyyMMddHHmmss.f"));
            Assert.AreEqual(model1.DateTimeOffsetThing.ToUniversalTime().ToString("yyyyMMddHHmmss.fzzz"), model2.DateTimeOffsetThing.ToUniversalTime().ToString("yyyyMMddHHmmss.fzzz"));
            Assert.AreEqual((int)model1.TimeSpanThing.TotalMilliseconds, (int)model2.TimeSpanThing.TotalMilliseconds);
            Assert.AreEqual(model1.GuidThing, model2.GuidThing);

            Assert.AreEqual(model1.ByteNullableThing, model2.ByteNullableThing);
            Assert.AreEqual(model1.Int16NullableThing, model2.Int16NullableThing);
            Assert.AreEqual(model1.Int32NullableThing, model2.Int32NullableThing);
            Assert.AreEqual(model1.Int64NullableThing, model2.Int64NullableThing);
            Assert.AreEqual(model1.SingleNullableThing, model2.SingleNullableThing);
            Assert.AreEqual(model1.DoubleNullableThing, model2.DoubleNullableThing);
            Assert.AreEqual(model1.DecimalNullableThing, model2.DecimalNullableThing);
            Assert.AreEqual(model1.CharNullableThing, model2.CharNullableThing);
            Assert.AreEqual(model1.DateTimeNullableThing.Value.ToUniversalTime().ToString("yyyyMMddHHmmss.f"), model2.DateTimeNullableThing.Value.ToUniversalTime().ToString("yyyyMMddHHmmss.f"));
            Assert.AreEqual(model1.DateTimeOffsetNullableThing.Value.ToUniversalTime().ToString("yyyyMMddHHmmss.fzzz"), model2.DateTimeOffsetNullableThing.Value.ToUniversalTime().ToString("yyyyMMddHHmmss.fzzz"));
            Assert.AreEqual((int)model1.TimeSpanNullableThing.Value.TotalMilliseconds, (int)model2.TimeSpanNullableThing.Value.TotalMilliseconds);
            Assert.AreEqual(model1.GuidNullableThing, model2.GuidNullableThing);

            Assert.IsNull(model1.ByteNullableThingNull);
            Assert.IsNull(model1.Int16NullableThingNull);
            Assert.IsNull(model1.Int32NullableThingNull);
            Assert.IsNull(model1.Int64NullableThingNull);
            Assert.IsNull(model1.SingleNullableThingNull);
            Assert.IsNull(model1.DoubleNullableThingNull);
            Assert.IsNull(model1.DecimalNullableThingNull);
            Assert.IsNull(model1.CharNullableThingNull);
            Assert.IsNull(model1.DateTimeNullableThingNull);
            Assert.IsNull(model1.DateTimeOffsetNullableThingNull);
            Assert.IsNull(model1.TimeSpanNullableThingNull);
            Assert.IsNull(model1.GuidNullableThingNull);

            Assert.IsNull(model2.ByteNullableThingNull);
            Assert.IsNull(model2.Int16NullableThingNull);
            Assert.IsNull(model2.Int32NullableThingNull);
            Assert.IsNull(model2.Int64NullableThingNull);
            Assert.IsNull(model2.SingleNullableThingNull);
            Assert.IsNull(model2.DoubleNullableThingNull);
            Assert.IsNull(model2.DecimalNullableThingNull);
            Assert.IsNull(model2.CharNullableThingNull);
            Assert.IsNull(model2.DateTimeNullableThingNull);
            Assert.IsNull(model2.DateTimeOffsetNullableThingNull);
            Assert.IsNull(model2.TimeSpanNullableThingNull);
            Assert.IsNull(model2.GuidNullableThingNull);

            Assert.AreEqual(model1.StringThing, model2.StringThing);

            Assert.IsNull(model1.StringThingNull);
            Assert.IsNull(model2.StringThingNull);

            if (model1.BytesThing != null)
            {
                Assert.IsNotNull(model2.BytesThing);
                Assert.AreEqual(model1.BytesThing.Length, model2.BytesThing.Length);
                for (var i = 0; i < model1.BytesThing.Length; i++)
                    Assert.AreEqual(model1.BytesThing[i], model2.BytesThing[i]);
            }
            else
            {
                Assert.IsNull(model2.BytesThing);
            }

            Assert.IsNull(model1.BytesThingNull);
            Assert.IsNull(model2.BytesThingNull);
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
            _ = provider.Query(new QuerySingle<T>(x => x.TimeSpanThing > TimeSpan.FromMilliseconds(123)));

            //string like
            _ = provider.Query(new QuerySingle<T>(x => x.StringThing.Contains("World")));

            //LINQ contains
            _ = provider.Query(new QuerySingle<T>(x => keyArray.Contains(x.KeyA)));

            //LINQ any
            _ = provider.Query(new QuerySingle<T>(x => x.RelationB.Any(y => y.RelationAKey == relationModel.RelationAKey)));
        }
    }
}
