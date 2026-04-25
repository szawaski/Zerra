// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository.Test
{
    public class TestTypesModelRuleProvider<TContext> : BaseTransactStoreRuleProvider<ITransactStoreProvider<TestTypesModel>, TestTypesModel>
      where TContext : DataContext, new()
    {
        public TestTypesModelRuleProvider()
            : base(new TransactStoreProvider<TContext, TestTypesModel>()) { }
    }
}
