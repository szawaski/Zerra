using System.Collections.Generic;
using Zerra.Repository;
using ZerraDemo.Domain.Ledger1.DataModels;

namespace ZerraDemo.Domain.Ledger1.EventStore
{
    [TransactStoreEntity(typeof(Ledger1AccountDataModel))]
    public class Ledger1DataContext : DataContextSelector
    {
        protected override ICollection<DataContext> GetDataContexts()
        {
            return new DataContext[]
            {
                new Ledger1MsSqlDataContext(),
                new Ledger1PostgreSqlDataContext(),
                new Ledger1MySqlDataContext()
            };
        }
    }
}
