// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections.Generic;
using System.Linq;
using Zerra.Repository;

namespace Zerra.TestDev
{
    public static class TestLinq
    {
        public static void Test()
        {
            //var testBasic = Repo.Query(new QueryMany<TestCustomerSqlModel>());

            //var testOrder1 = Repo.Query(new QueryMany<TestCustomerSqlModel>(
            //    LinqOrder<TestCustomerSqlModel>.Create(x => x.CustomerID, true, x => x.Name)
            //));

            //var testOrder2 = Repo.Query(new QueryMany<TestOrderSqlModel>(
            //    LinqOrder<TestOrderSqlModel>.Create(x => x.Customer.Name + "2")
            //));

            var testDateParts = Repo.Query(new QueryMany<TestOrderSqlModel>(x =>
                x.Date.Year == 2020 &&
                x.Date.Month == 1
            ));

            var testRelationMany_Any = Repo.Query(new QueryMany<TestCustomerSqlModel>(x =>
                x.CustomerID > 0 &&
                x.Orders.Any(y => y.Amount > 0)
            ));

            var testRelationMany_NotAny = Repo.Query(new QueryMany<TestCustomerSqlModel>(x =>
                x.CustomerID > 0 &&
                !x.Orders.Any(y => y.Amount > 0)
            ));

            var testRelationMany_AnyChained = Repo.Query(new QueryMany<TestOrderSqlModel>(x =>
                x.Customer.Orders.Any(y => y.Amount > 0)
            ));

            var testRelationMany_All = Repo.Query(new QueryMany<TestCustomerSqlModel>(x =>
                x.CustomerID > 0 &&
                x.Orders.All(y => y.Amount > 0)
            ));

            var testRelationMany_NotAll = Repo.Query(new QueryMany<TestCustomerSqlModel>(x =>
                x.CustomerID > 0 &&
                !x.Orders.All(y => y.Amount > 0)
            ));

            var testRelationMany_AllChained = Repo.Query(new QueryMany<TestOrderSqlModel>(x =>
                x.Customer.Orders.All(y => y.Amount > 0)
            ));

            var testRelationMany_Count1 = Repo.Query(new QueryMany<TestCustomerSqlModel>(x =>
                x.CustomerID > 0 &&
                x.Credit > x.Orders.Count(y => y.Amount > 0)
            ));

            var testRelationMany_Count2 = Repo.Query(new QueryMany<TestCustomerSqlModel>(x =>
                x.CustomerID > 0 &&
                x.Credit > x.Orders.Length
            ));

            var testRelationMany_CountChained = Repo.Query(new QueryMany<TestOrderSqlModel>(x =>
                x.Amount > x.Customer.Orders.Length
            ));

            var testRelationSingle = Repo.Query(new QueryMany<TestOrderSqlModel>(x =>
                x.Customer.Credit > 0
            ));



            var ids = new int[] { 10, 11, 12, 13, 14 };
            var testContains = Repo.Query(new QueryMany<TestCustomerSqlModel>(x =>
                ids.Contains(x.CustomerID)
            ));

            var ids2 = new List<int> { 10, 11, 12, 13, 14 };
            var testContains2 = Repo.Query(new QueryMany<TestCustomerSqlModel>(x =>
                ids2.Contains(x.CustomerID)
            ));

            var testStringContains = Repo.Query(new QueryMany<TestCustomerSqlModel>(x =>
                x.Name.Contains("John")
            ));
        }
    }
}
