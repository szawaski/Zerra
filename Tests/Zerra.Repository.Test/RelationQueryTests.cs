// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using Zerra.Repository.Memory;

namespace Zerra.Repository.Test
{
    public class RelationQueryTests
    {
        [Entity("QueryOwner")]
        public sealed class Owner
        {
            [Identity]
            public Guid ID { get; set; }
            public string? Name { get; set; }
            public int? PetID { get; set; }

            [Relation(nameof(PetID))]
            public Pet? Pet { get; set; }

            [Relation(nameof(Toy.OwnerID))]
            public List<Toy>? Toys { get; set; }
        }

        [Entity("QueryPet")]
        public sealed class Pet
        {
            [Identity(true)]
            public int ID { get; set; }
            public string? Name { get; set; }
            public int Age { get; set; }
        }

        [Entity("QueryToy")]
        public sealed class Toy
        {
            [Identity]
            public Guid ID { get; set; }
            public Guid OwnerID { get; set; }
            public string? Name { get; set; }
        }

        [Fact]
        public async Task GraphLoadsRelations()
        {
            foreach (var eventStore in new[] { false, true })
            {
                var (repo, withPet, withoutPet) = Create(eventStore);
                var graph = new Graph<Owner>(true, x => x.Pet, x => x.Toys);

                var read = repo.Query<Owner>(graph).Where(x => x.ID == withPet.ID).ToArray().Single();
                Assert.Equal("rex", read.Pet!.Name);
                Assert.Equal(withPet.Toys!.Count, read.Toys!.Count);

                var other = await repo.Query<Owner>(graph).SingleOrDefaultAsync(x => x.ID == withoutPet.ID, cancellationToken: TestContext.Current.CancellationToken);
                Assert.Null(other!.Pet);
                Assert.Empty(other.Toys!);

                //the stored models aren't handed out, and what the graph leaves out isn't read
                var named = repo.Query<Owner>(new Graph<Owner>(x => x.Pet!.Name)).Where(x => x.ID == withPet.ID).ToArray().Single();
                Assert.Null(named.Name);
                Assert.Equal("rex", named.Pet!.Name);
                Assert.Equal(0, named.Pet.Age);
            }
        }

        //the model can only be read through a relation back to it
        [Fact]
        public void SelectManyResultNeedsARelationBack()
        {
            var (repo, withPet, _) = Create();
            Assert.Equal(withPet.Toys!.Select(x => x.Name).Order(), repo.Query<Owner>().SelectMany(x => x.Toys!, (x, t) => t.Name).ToArray().Order());
            _ = Assert.Throws<NotSupportedException>(() => repo.Query<Owner>().SelectMany(x => x.Toys!, (x, t) => new { x.Name, Toy = t.Name }).ToArray());
        }

        private static (IRepo, Owner, Owner) Create(bool eventStoreOwner = false)
        {
            var engine = new MemoryEngine();
            var repo = Repo.New();
            repo.AddProvider<Owner>(eventStoreOwner ? new EventStoreAsTransactStoreProvider<Owner>(engine) : new TransactStoreProvider<Owner>(engine));
            repo.AddProvider(new TransactStoreProvider<Pet>(engine));
            repo.AddProvider(new TransactStoreProvider<Toy>(engine));

            var full = new Graph<Owner>(true, x => x.Pet, x => x.Toys);
            var withPet = new Owner { ID = Guid.NewGuid(), Name = "with pet", Pet = new Pet { Name = "rex", Age = 3 }, Toys = [new Toy { ID = Guid.NewGuid(), Name = "ball" }, new Toy { ID = Guid.NewGuid(), Name = "rope" }] };
            var withoutPet = new Owner { ID = Guid.NewGuid(), Name = "without pet", Toys = [] };
            repo.Create(withPet, full);
            repo.Create(withoutPet, full);
            return (repo, withPet, withoutPet);
        }

        [Fact]
        public async Task PartialGraphs_LoadRelations()
        {
            var (repo, withPet, withoutPet) = Create();
            Assert.Null(withoutPet.PetID);

            //the select doesn't name PetID or ID, the relations still load
            var owner = repo.Query<Owner>(new Graph<Owner>(x => x.Name, x => x.Pet, x => x.Toys)).Where(x => x.ID == withPet.ID).SingleOrDefault()!;
            Assert.Equal("rex", owner.Pet!.Name);
            Assert.Equal(["ball", "rope"], owner.Toys!.Select(x => x.Name).Order());

            //a member of a single relation joins, and the rows of a collection read only what they select
            var names = repo.Query<Owner>().Where(x => x.ID == withPet.ID).Select(x => new { PetName = x.Pet!.Name, Toys = x.Toys!.Select(t => new Toy { Name = t.Name }).ToList() }).Single();
            Assert.Equal("rex", names.PetName);
            Assert.Equal(2, names.Toys.Count);
            var async = await repo.Query<Owner>().Where(x => x.ID == withPet.ID).Select(x => new { PetName = x.Pet!.Name, Toys = x.Toys!.Select(t => new Toy { Name = t.Name }).ToList() }).FirstAsync(cancellationToken: TestContext.Current.CancellationToken);
            Assert.Equal(2, async.Toys.Count);

            var none = repo.Query<Owner>(new Graph<Owner>(x => x.Pet, x => x.Toys)).Where(x => x.ID == withoutPet.ID).SingleOrDefault()!;
            Assert.Null(none.Pet);
            Assert.Empty(none.Toys!);
        }

        [Fact]
        public async Task NullRelation_ClearsTheForeignKey()
        {
            var (repo, withPet, _) = Create();
            withPet.Pet = null;
            repo.Update(withPet, new Graph<Owner>(true, x => x.Pet));
            Assert.Null(repo.Query<Owner>().SingleOrDefault(x => x.ID == withPet.ID)!.PetID);

            var other = new Owner { ID = Guid.NewGuid(), Name = "async", Pet = new Pet { Name = "tom" } };
            await repo.CreateAsync(other, new Graph<Owner>(true, x => x.Pet), TestContext.Current.CancellationToken);
            other.Pet = null;
            await repo.UpdateAsync(other, new Graph<Owner>(true, x => x.Pet), TestContext.Current.CancellationToken);
            Assert.Null((await repo.Query<Owner>().SingleOrDefaultAsync(x => x.ID == other.ID, cancellationToken: TestContext.Current.CancellationToken))!.PetID);
        }

        //a relation the condition or order reads loads to run it, and the states read without it as asked
        [Fact]
        public void TemporalQueries_ReadRelationsWithoutReturningThem()
        {
            var (repo, withPet, _) = Create(eventStoreOwner: true);

            var plain = Assert.Single(repo.TemporalQuery<Owner>().Where(x => x.ID == withPet.ID).ToList());
            Assert.Null(plain.Pet);
            Assert.Equal("with pet", plain.Name);
            var filtered = Assert.Single(repo.TemporalQuery<Owner>().Where(x => x.ID == withPet.ID && x.Pet!.Name == "rex").ToList());
            Assert.Null(filtered.Pet);
            Assert.Empty(repo.TemporalQuery<Owner>().Where(x => x.ID == withPet.ID && x.Pet!.Name == "other").ToList());
            var ordered = Assert.Single(repo.TemporalQuery<Owner>().Where(x => x.ID == withPet.ID).OrderBy(x => x.Pet!.Name).ToList());
            Assert.Null(ordered.Pet);
            Assert.Equal("rex", repo.TemporalQuery<Owner>().Where(x => x.ID == withPet.ID).Select(x => x.Pet!.Name).Single());
        }

        [Fact]
        public async Task EventQueries_LoadRelations()
        {
            var (repo, withPet, _) = Create(eventStoreOwner: true);
            var graph = new Graph<Owner>(true, x => x.Pet, x => x.Toys);

            var events = repo.EventQuery<Owner>(TemporalOrder.Oldest, graph: graph).Where(x => x.Model.ID == withPet.ID).ToList();
            Assert.Equal("rex", Assert.Single(events).Model.Pet!.Name);
            Assert.Equal("rex", repo.EventQuery<Owner>(graph: graph).Where(x => x.Model.ID == withPet.ID).FirstOrDefault()!.Model.Pet!.Name);
            Assert.Equal(2, repo.EventQuery<Owner>(graph: graph).Where(x => x.Model.ID == withPet.ID).SingleOrDefault()!.Model.Toys!.Count);
            Assert.Equal("rex", Assert.Single(await repo.EventQuery<Owner>(TemporalOrder.Oldest, graph: graph).Where(x => x.Model.ID == withPet.ID).ToListAsync(cancellationToken: TestContext.Current.CancellationToken)).Model.Pet!.Name);
            Assert.Equal("rex", (await repo.EventQuery<Owner>(graph: graph).Where(x => x.Model.ID == withPet.ID).FirstOrDefaultAsync(cancellationToken: TestContext.Current.CancellationToken))!.Model.Pet!.Name);
            Assert.Equal(2, (await repo.EventQuery<Owner>(graph: graph).Where(x => x.Model.ID == withPet.ID).SingleOrDefaultAsync(cancellationToken: TestContext.Current.CancellationToken))!.Model.Toys!.Count);
        }
    }
}
