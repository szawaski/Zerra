// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository.Test
{
    public class TestTypesModelEventRuleProvider<TContext> : BaseTransactStoreRuleProvider<ITransactStoreProvider<TestTypesModel>, TestTypesModel>
      where TContext : DataContext, new()
    {
        public TestTypesModelEventRuleProvider()
            : base(new EventStoreAsTransactStoreProvider<TContext, TestTypesModel>()) { }
    }
}
