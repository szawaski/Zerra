// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.Repository;
using Zerra.Repository.Sql;
using Zerra.Repository.MsSql;

namespace Zerra.DevTest
{
    [DataSourceEntity("Customer")]
    public class TestCustomerSqlModel
    {
        [Identity]
        public int CustomerID { get; set; }
        public string Name { get; set; }
        public decimal Credit { get; set; }

        [DataSourceRelation(nameof(CustomerID))]
        public TestOrderSqlModel[] Orders { get; set; }
    }

    [DataSourceEntity("Order")]
    public class TestOrderSqlModel
    {
        [Identity]
        public int OrderID { get; set; }
        public int CustomerID { get; set; }
        public DateTime Date { get; set; }
        public string Item { get; set; }
        public decimal Amount { get; set; }

        [DataSourceRelation(nameof(CustomerID))]
        public TestCustomerSqlModel Customer { get; set; }
    }

    public class TestSqlDataContext : MsSqlDataContext
    {
        protected override bool DisableAssureDataStore => false;
        public override string ConnectionString => "data source=.;initial catalog=Test;integrated security=True;MultipleActiveResultSets=True;";
    }

    public abstract partial class BaseTestSqlProvider<TModel> : SqlProvider<TestSqlDataContext, TModel> where TModel : class, new()
    {
        protected override bool DisableQueryLinking => true;
        protected override bool DisableEventLinking => true;
        protected override bool DisablePersistLinking => true;
    }
    public class TestCustomerSqlProvider : BaseTestSqlProvider<TestCustomerSqlModel> { }
    public class TestOrderSqlProvider : BaseTestSqlProvider<TestOrderSqlModel> { }

    //public abstract partial class BaseTestDualEventStoreProvider<TModel> : BaseDataDualProvider<EventStoreProvider<TestEventStoreDataContext, TModel>, IDataProvider<TModel>, TModel> where TModel : class, new() { }
    //public class TestCustomerEventStoreProvider : BaseTestDualEventStoreProvider<TestCustomerSqlModel> { }
    //public class TestOrderEventStoreQueryProvider : BaseTestDualEventStoreProvider<TestOrderSqlModel> { }
}