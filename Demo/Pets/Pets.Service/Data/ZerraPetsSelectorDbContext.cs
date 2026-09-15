using Zerra.Repository;

namespace Pets.Service.Data
{
    public class ZerraPetsSelectorDbContext : DataContextSelector
    {
        protected override IEnumerable<DataContext> LoadDataContexts()
        {
            yield return new ZerraPetsMemoryContext();
            yield return new ZerraPetsMsSqlContext();
            yield return new ZerraPetsMySqlContext();
            yield return new ZerraPetsMariaDbContext();
            yield return new ZerraPetsPostgreSqlContext();
        }
    }
}