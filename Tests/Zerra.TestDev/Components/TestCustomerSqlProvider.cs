﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.Repository;
using Zerra.Repository.MsSql;

namespace Zerra.TestDev
{
    [Entity("Customer")]
    public class TestCustomerSqlModel
    {
        [Identity]
        public int CustomerID { get; set; }
        public string Name { get; set; }
        public decimal Credit { get; set; }

        [Relation(nameof(CustomerID))]
        public TestOrderSqlModel[] Orders { get; set; }
    }

    [Entity("Order")]
    public class TestOrderSqlModel
    {
        [Identity]
        public int OrderID { get; set; }
        public int CustomerID { get; set; }
        public DateTime Date { get; set; }
        public string Item { get; set; }
        public decimal Amount { get; set; }

        [Relation(nameof(CustomerID))]
        public TestCustomerSqlModel Customer { get; set; }
    }

    public class TestSqlDataContext : MsSqlDataContext
    {
        protected override DataStoreGenerationType DataStoreGenerationType => DataStoreGenerationType.CodeFirst;
        public override string ConnectionString => "data source=.;initial catalog=Test;integrated security=True;MultipleActiveResultSets=True;";
    }

    public abstract partial class BaseTestSqlProvider<TModel> : TransactStoreProvider<TestSqlDataContext, TModel> where TModel : class, new()
    {

    }
    public class TestCustomerSqlProvider : BaseTestSqlProvider<TestCustomerSqlModel> { }
    public class TestOrderSqlProvider : BaseTestSqlProvider<TestOrderSqlModel> { }

    //public abstract partial class BaseTestDualEventStoreProvider<TModel> : BaseDataDualProvider<EventStoreProvider<TestEventStoreDataContext, TModel>, IDataProvider<TModel>, TModel> where TModel : class, new() { }
    //public class TestCustomerEventStoreProvider : BaseTestDualEventStoreProvider<TestCustomerSqlModel> { }
    //public class TestOrderEventStoreQueryProvider : BaseTestDualEventStoreProvider<TestOrderSqlModel> { }
}