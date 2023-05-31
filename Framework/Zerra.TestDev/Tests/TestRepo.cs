// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.Repository;

namespace Zerra.TestDev
{
    public static class TestRepo
    {
        public static void Test()
        {
            var customerID = 4;
            if (!Repo.Query(new QueryAny<TestCustomerSqlModel>(x => x.CustomerID == customerID)))
            {
                var createModel = new TestCustomerSqlModel()
                {
                    CustomerID = customerID,
                    Name = "John Galt",
                    Credit = 0
                };
                Repo.Persist(new Create<TestCustomerSqlModel>("CreateCustomer", createModel));
            }

            var updateModel = new TestCustomerSqlModel()
            {
                CustomerID = customerID,
                Credit = 2000
            };
            Repo.Persist(new Update<TestCustomerSqlModel>("UpdateCustomer", updateModel, new Graph<TestCustomerSqlModel>(x => x.Credit)));


            var test1 = Repo.Query(new QuerySingle<TestCustomerSqlModel>(x => x.CustomerID == customerID));

            var test2StartDate = new DateTime(2019, 1, 1);
            var test2EndDate = new DateTime(2020, 12, 31);
            var test2 = Repo.Query(new TemporalQueryMany<TestCustomerSqlModel>(test2StartDate, test2EndDate, x => x.CustomerID == customerID));

            var test3 = Repo.Query(new EventQueryMany<TestCustomerSqlModel>((DateTime?)null, null, x => x.CustomerID == customerID));

            var test4 = Repo.Query(new EventQueryMany<TestCustomerSqlModel>(new DateTime(2019, 9, 1), null, x => x.CustomerID == customerID));
        }
    }
}
