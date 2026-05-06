using Zerra.Repository;

namespace Pets.Service.Data
{
    public class ZerraPetsDbContext : DataContextSelector
    {
        protected override ICollection<DataContext> LoadDataContexts() =>
        [
            new ZerraPetsMemoryContext(),
            new ZerraPetsMsSqlContext(),
            new ZerraPetsMySqlContext(),
            new ZerraPetsMariaDbContext(),
            new ZerraPetsPostgreSqlContext()
        ];
    }
}