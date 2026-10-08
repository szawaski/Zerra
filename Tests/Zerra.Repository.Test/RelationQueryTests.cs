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

            //the graph doesn't name PetID or ID, the relations still load
            var graph = new Graph<Owner>(x => x.Name, x => x.Pet, x => x.Toys);
            var owner = repo.Single(x => x.ID == withPet.ID, graph: graph)!;
            Assert.Equal("rex", owner.Pet!.Name);
            Assert.Equal(["ball", "rope"], owner.Toys!.Select(x => x.Name).Order());

            //a child graph loads only those members of the relation
            var petName = repo.First(x => x.ID == withPet.ID, new Graph<Owner>(x => x.Pet!.Name))!;
            Assert.Equal("rex", petName.Pet!.Name);
            Assert.Equal(0, petName.Pet.Age);

            var syncChild = repo.Single(x => x.ID == withPet.ID, new Graph<Owner>(x => x.Pet!.Name, x => x.Toys!.Select(t => t.Name)))!;
            Assert.Equal(2, syncChild.Toys!.Count);
            var async = (await repo.FirstAsync(x => x.ID == withPet.ID, graph: graph))!;
            Assert.Equal(2, async.Toys!.Count);
            var asyncChild = (await repo.SingleAsync(x => x.ID == withPet.ID, new Graph<Owner>(x => x.Pet!.Name, x => x.Toys!.Select(t => t.Name))))!;
            Assert.Equal(0, asyncChild.Pet!.Age);
            Assert.Equal(2, asyncChild.Toys!.Count);

            var none = repo.Single(x => x.ID == withoutPet.ID, graph)!;
            Assert.Null(none.Pet);
            Assert.Empty(none.Toys!);
        }

        [Fact]
        public async Task NullRelation_ClearsTheForeignKey()
        {
            var (repo, withPet, _) = Create();
            withPet.Pet = null;
            repo.Update(withPet, new Graph<Owner>(true, x => x.Pet));
            Assert.Null(repo.Single<Owner>(x => x.ID == withPet.ID)!.PetID);

            var other = new Owner { ID = Guid.NewGuid(), Name = "async", Pet = new Pet { Name = "tom" } };
            await repo.CreateAsync(other, new Graph<Owner>(true, x => x.Pet));
            other.Pet = null;
            await repo.UpdateAsync(other, new Graph<Owner>(true, x => x.Pet));
            Assert.Null((await repo.SingleAsync<Owner>(x => x.ID == other.ID))!.PetID);
        }

        [Fact]
        public async Task EventQueries_LoadRelations()
        {
            var (repo, withPet, _) = Create(eventStoreOwner: true);
            var graph = new Graph<Owner>(true, x => x.Pet, x => x.Toys);

            var events = repo.EventMany<Owner>(TemporalOrder.Oldest, (ulong?)null, null, null, null, x => x.ID == withPet.ID, graph: graph);
            Assert.Equal("rex", Assert.Single(events).Model.Pet!.Name);
            Assert.Equal("rex", repo.EventFirst<Owner>(TemporalOrder.Newest, (ulong?)null, null, null, null, x => x.ID == withPet.ID, graph: graph)!.Model.Pet!.Name);
            Assert.Equal(2, repo.EventSingle<Owner>(TemporalOrder.Newest, (ulong?)null, null, null, null, x => x.ID == withPet.ID, graph: graph)!.Model.Toys!.Count);
            Assert.Equal("rex", Assert.Single(await repo.EventManyAsync<Owner>(TemporalOrder.Oldest, (ulong?)null, null, null, null, x => x.ID == withPet.ID, graph: graph)).Model.Pet!.Name);
            Assert.Equal("rex", (await repo.EventFirstAsync<Owner>(TemporalOrder.Newest, (ulong?)null, null, null, null, x => x.ID == withPet.ID, graph: graph))!.Model.Pet!.Name);
            Assert.Equal(2, (await repo.EventSingleAsync<Owner>(TemporalOrder.Newest, (ulong?)null, null, null, null, x => x.ID == withPet.ID, graph: graph))!.Model.Toys!.Count);
        }
    }
}
