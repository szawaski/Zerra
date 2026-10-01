// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository.Test
{
    public class TestTypesModelEventRuleProvider : BaseTransactStoreRuleProvider<ITransactStoreProvider<TestTypesModel>, TestTypesModel>
    {
        public TestTypesModelEventRuleProvider(IEventStoreEngine engine)
            : base(new EventStoreAsTransactStoreProvider<TestTypesModel>(engine)) { }
    }
}
