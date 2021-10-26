using System.Collections.Generic;
using Zerra.Repository;

namespace ZerraDemo.Domain.Ledger1.EventStore
{
    public class Ledger1DataContext : DataContextSelector
    {
        protected override ICollection<DataContext> GetDataContexts()
        {
            return new DataContext[]
            {
                new Ledger1PostgreSqlDataContext(),
                new Ledger1MsSqlDataContext(),
                new Ledger1MySqlDataContext()
            };
        }
    }
}
