// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using Zerra.Repository.Memory;

namespace Zerra.Repository.Test
{
    //deletes are sent in batches of 1028 for a single identity, a count on a batch boundary has no empty batch left over
    public class DeleteBatchTests
    {
        [Entity("DeleteBatchModel")]
        public sealed class DeleteBatchModel
        {
            [Identity]
            public Guid ID { get; set; }
        }

        public static TheoryData<int> Counts => new() { 1, 1027, 1028, 1029, 2056 };

        private static (IRepo, DeleteBatchModel[]) Create(int count)
        {
            var repo = Repo.New();
            repo.AddProvider(new TransactStoreProvider<DeleteBatchModel>(new MemoryEngine()));
            var models = Enumerable.Range(0, count).Select(_ => new DeleteBatchModel { ID = Guid.NewGuid() }).ToArray();
            repo.Create<DeleteBatchModel>(models);
            return (repo, models);
        }

        [Theory]
        [MemberData(nameof(Counts))]
        public void Delete_AnyCount(int count)
        {
            var (repo, models) = Create(count);
            repo.Delete<DeleteBatchModel>(models);
            Assert.Equal(0, repo.Count<DeleteBatchModel>(x => true));

            (repo, models) = Create(count);
            repo.DeleteByID<DeleteBatchModel>(models.Select(x => x.ID).ToArray());
            Assert.Equal(0, repo.Count<DeleteBatchModel>(x => true));
        }

        [Theory]
        [MemberData(nameof(Counts))]
        public async Task DeleteAsync_AnyCount(int count)
        {
            var (repo, models) = Create(count);
            await repo.DeleteAsync<DeleteBatchModel>(models);
            Assert.Equal(0, await repo.CountAsync<DeleteBatchModel>(x => true));

            (repo, models) = Create(count);
            await repo.DeleteByIDAsync<DeleteBatchModel>(models.Select(x => x.ID).ToArray());
            Assert.Equal(0, await repo.CountAsync<DeleteBatchModel>(x => true));
        }
    }
}
