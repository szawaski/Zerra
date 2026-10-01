// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using System.Linq.Expressions;

namespace Zerra.Repository.Test
{
    public class TestRelationsRule1Provider : BaseTransactStoreRuleProvider<ITransactStoreProvider<TestRelationsModel>, TestRelationsModel>
    {
        public TestRelationsRule1Provider(ITransactStoreEngine engine)
            : base(new TransactStoreProvider<TestRelationsModel>(engine)) { }

        public override LambdaExpression WhereExpression(Graph graph)
        {
            return (TestRelationsModel x) => x.SomeValue != "Test1";
        }

        public override IEnumerable OnGet(IEnumerable models, Graph graph)
        {
            foreach (TestRelationsModel model in models)
            {
                model.SomeValue += " OnGet1";
            }
            return models;
        }
    }
}
