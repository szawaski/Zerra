// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using System.Linq.Expressions;

namespace Zerra.Repository.Test
{
    public class TestRelationsRule1Provider<TContext> : BaseTransactStoreRuleProvider<ITransactStoreProvider<TestRelationsModel>, TestRelationsModel>
        where TContext : DataContext, new()
    {
        public TestRelationsRule1Provider()
            : base(new TransactStoreProvider<TContext, TestRelationsModel>()) { }

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
