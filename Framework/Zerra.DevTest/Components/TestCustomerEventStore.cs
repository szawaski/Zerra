// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.Repository;
using Zerra.Repository.EventStore;
using Zerra.Repository.EventStoreDB;

namespace Zerra.DevTest
{
    [DataSourceEntity("Customer")]
    public class TestCustomerEventStoreModel
    {
        [Identity]
        public int CustomerID { get; set; }
        public string Name { get; set; }
        public decimal Credit { get; set; }

        [DataSourceRelation(nameof(CustomerID))]
        public TestOrderEventStoreModel[] Orders { get; set; }
    }

    [DataSourceEntity("Order")]
    public class TestOrderEventStoreModel
    {
        [Identity]
        public int OrderID { get; set; }
        public int CustomerID { get; set; }
        public DateTime Date { get; set; }
        public string Item { get; set; }
        public decimal Amount { get; set; }
    }

    public class TestEventStoreDataContext : EventStoreDBDataContext
    {
        protected override bool DisableBuildStoreFromModels => false;
        public override string ConnectionString => connectionString;
        public override bool Insecure => true;
        private readonly string connectionString;
        public TestEventStoreDataContext()
        {
            this.connectionString = "localhost:2113";
        }
    }

    public abstract partial class EventStoreBaseQueryProvider<TModel> : EventStoreProvider<TestEventStoreDataContext, TModel> where TModel : class, new() { }
    public class TestCustomerQueryProvider : EventStoreBaseQueryProvider<TestCustomerEventStoreModel> { }
}
