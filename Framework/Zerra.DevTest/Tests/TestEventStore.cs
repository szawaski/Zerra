// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.Repository;

namespace Zerra.DevTest
{
    public static class TestEventStore
    {
        public static void Test()
        {
            //var createModel = new TestCustomerEventStoreModel()
            //{
            //    CustomerID = 1,
            //    Name = "John Galt",
            //    Credit = 0
            //};
            //Repo.Persist(new Create<TestCustomerEventStoreModel>("CreateCustomer", createModel));

            var updateModel = new TestCustomerEventStoreModel()
            {
                CustomerID = 1,
                Credit = 2000
            };
            Repo.Persist(new Update<TestCustomerEventStoreModel>("UpdateCustomer", updateModel, new Graph<TestCustomerEventStoreModel>(x => x.Credit)));

            //for (var i = 1; i <= 50; i++)
            //{
            //    var updateModel = new TestCustomerModel()
            //    {
            //        CustomerID = 1,
            //        Credit = i
            //    };
            //    Repo.Persist(new Update<TestCustomerModel>("UpdateCustomer", updateModel, new Graph<TestCustomerModel>(x => x.Credit)));
            //}

            var test1 = Repo.Query(new QuerySingle<TestCustomerEventStoreModel>(x => x.CustomerID == 1));

            var test2StartDate = new DateTime(2019, 1, 1);
            var test2EndDate = new DateTime(2019, 12, 31);
            var test2 = Repo.Query(new TemporalQueryMany<TestCustomerEventStoreModel>(test2StartDate, test2EndDate, x => x.CustomerID == 1));

            var test3 = Repo.Query(new EventQueryMany<TestCustomerEventStoreModel>((DateTime?)null, null, x => x.CustomerID == 1));

            var test4 = Repo.Query(new EventQueryMany<TestCustomerEventStoreModel>(new DateTime(2019, 9, 1), null, x => x.CustomerID == 1));
        }
    }
}
