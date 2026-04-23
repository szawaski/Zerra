using Zerra.Repository;

namespace Pets.Service.Data
{
    public class ZerraPetsDbContext : DataContextSelector
    {
        protected override ICollection<DataContext> LoadDataContexts() =>
        [
            new ZerraPetsMsSqlContext(),
            new ZerraPetsMySqlContext(),
            new ZerraPetsPostgreSqlContext()
        ];
    }
}