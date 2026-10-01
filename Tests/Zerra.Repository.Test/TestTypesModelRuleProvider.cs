// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository.Test
{
    public class TestTypesModelRuleProvider : BaseTransactStoreRuleProvider<ITransactStoreProvider<TestTypesModel>, TestTypesModel>
    {
        public TestTypesModelRuleProvider(ITransactStoreEngine engine)
            : base(new TransactStoreProvider<TestTypesModel>(engine)) { }
    }
}
