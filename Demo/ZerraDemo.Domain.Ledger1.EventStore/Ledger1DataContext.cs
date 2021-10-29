using System.Collections.Generic;
using Zerra.Repository;
using ZerraDemo.Domain.Ledger1.DataModels;

namespace ZerraDemo.Domain.Ledger1.EventStore
{
    [ApplyEntity(typeof(Ledger1AccountDataModel))]
    public class Ledger1DataContext : DataContextSelector<ITransactStoreEngine>
    {
        protected override ICollection<DataContext<ITransactStoreEngine>> GetDataContexts()
        {
            return new DataContext<ITransactStoreEngine>[]
            {
                new Ledger1MsSqlDataContext(),
                new Ledger1PostgreSqlDataContext(),
                new Ledger1MySqlDataContext()
            };
        }
    }
}
