// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using System.Linq.Expressions;
using Xunit;
using Zerra.Repository.Memory;
using static Zerra.Repository.Test.RelationPersistTests;

namespace Zerra.Repository.Test
{
    public class RuleProviderTests
    {
        private sealed class ParentRules(ITransactStoreProvider<Parent> next) : BaseTransactStoreRuleProvider<ITransactStoreProvider<Parent>, Parent>(next)
        {
            public List<string> Calls { get; } = new();

            public override LambdaExpression? WhereExpression(Graph? graph) => (Parent x) => !x.Name!.StartsWith("hidden");

            protected override IEnumerable OnCreate(IEnumerable models, Graph? graph)
            {
                foreach (Parent model in models)
                    model.Name += " created";
                Calls.Add("create");
                return models;
            }
            protected override IEnumerable OnUpdate(IEnumerable models, Graph? graph)
            {
                foreach (Parent model in models)
                    model.Name += " updated";
                Calls.Add("update");
                return models;
            }
            protected override ICollection OnDelete(ICollection identities)
            {
                Calls.Add($"delete {identities.Count}");
                return identities;
            }
            protected override void OnCreateComplete(IEnumerable models, Graph? graph) => Calls.Add("create complete");
            protected override void OnUpdateComplete(IEnumerable models, Graph? graph) => Calls.Add("update complete");
            protected override void OnDeleteComplete(ICollection identities) => Calls.Add("delete complete");
        }

        private static readonly Graph<Parent> withItems = new(true, x => x.Items);

        private static (IRepo, ParentRules) CreateRepo()
        {
            var engine = new MemoryEngine();
            var rules = new ParentRules(new TransactStoreProvider<Parent>(engine));
            var repo = Repo.New();
            repo.AddProvider(rules);
            repo.AddProvider(new TransactStoreProvider<Child>(engine));
            repo.AddProvider(new TransactStoreProvider<Item>(engine));
            return (repo, rules);
        }

        private static Parent NewParent(string name = "parent") => new()
        {
            ID = Guid.NewGuid(),
            Name = name,
            Items = [new Item { ID = Guid.NewGuid(), Name = "one" }],
        };

        [Fact]
        public void Sync_HooksAndRules()
        {
            var (repo, rules) = CreateRepo();
            var parent = NewParent();
            var hidden = NewParent("hidden");

            repo.Create(parent, withItems);
            Assert.Equal("parent created", repo.Single<Parent>(x => x.ID == parent.ID)!.Name);
            Assert.Single(repo.Many<Item>(x => x.ParentID == parent.ID));

            repo.Update(parent);
            Assert.Equal("parent created updated", repo.Single<Parent>(x => x.ID == parent.ID)!.Name);

            //the rule's where hides models from every query
            repo.Create(hidden);
            Assert.Null(repo.Single<Parent>(x => x.ID == hidden.ID));
            Assert.Empty(repo.Many<Parent>(x => x.ID == hidden.ID));
            Assert.Null(repo.First<Parent>(x => x.ID == hidden.ID));
            Assert.Equal(1, repo.Count<Parent>(x => true));
            Assert.False(repo.Any<Parent>(x => x.ID == hidden.ID));

            //a delete with a graph removes the related models too
            repo.Delete(parent, withItems);
            Assert.Null(repo.Single<Parent>(x => x.ID == parent.ID));
            Assert.Empty(repo.Many<Item>(x => x.ParentID == parent.ID));

            repo.DeleteByID<Parent>(hidden.ID);
            Assert.Equal(["create", "create complete", "update", "update complete", "create", "create complete", "delete 1", "delete complete", "delete 1", "delete complete"], rules.Calls);
        }

        [Fact]
        public async Task Async_HooksAndRules()
        {
            var (repo, rules) = CreateRepo();
            var parent = NewParent();
            var hidden = NewParent("hidden");

            await repo.CreateAsync(parent, withItems);
            Assert.Equal("parent created", (await repo.SingleAsync<Parent>(x => x.ID == parent.ID))!.Name);

            await repo.UpdateAsync(parent);
            Assert.Equal("parent created updated", (await repo.SingleAsync<Parent>(x => x.ID == parent.ID))!.Name);

            await repo.CreateAsync(hidden);
            Assert.Null(await repo.SingleAsync<Parent>(x => x.ID == hidden.ID));
            Assert.Empty(await repo.ManyAsync<Parent>(x => x.ID == hidden.ID));
            Assert.Null(await repo.FirstAsync<Parent>(x => x.ID == hidden.ID));
            Assert.Equal(1, await repo.CountAsync<Parent>(x => true));
            Assert.False(await repo.AnyAsync<Parent>(x => x.ID == hidden.ID));

            await repo.DeleteAsync(parent, withItems);
            Assert.Null(await repo.SingleAsync<Parent>(x => x.ID == parent.ID));
            Assert.Empty(await repo.ManyAsync<Item>(x => x.ParentID == parent.ID));

            await repo.DeleteByIDAsync<Parent>(hidden.ID);
            Assert.Equal(10, rules.Calls.Count);
        }
    }
}
