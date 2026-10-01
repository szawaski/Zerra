// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using System.Linq.Expressions;

namespace Zerra.Repository.Test
{
    public class TestRelationsRule2Provider : BaseTransactStoreRuleProvider<ITransactStoreProvider<TestRelationsModel>, TestRelationsModel>
    {
        public TestRelationsRule2Provider(ITransactStoreEngine engine)
            : base(new TestRelationsRule1Provider(engine)) { }

        public override LambdaExpression WhereExpression(Graph graph)
        {
            return (TestRelationsModel x) => x.SomeValue != "Test2";
        }

        public override IEnumerable OnGet(IEnumerable models, Graph graph)
        {
            foreach(TestRelationsModel model in models)
            {
                model.SomeValue += " OnGet2";
            }
            return models;
        }
    }
}
