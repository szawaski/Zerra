using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Zerra.CQRS;
using Zerra.Providers;
using ZerraDemo.Domain.Ledger1.Models;

namespace ZerraDemo.Domain.Ledger1
{
    [ServiceExposed]
    public interface ILedger1QueryProvider : IBaseProvider
    {
        Task<bool> HasBalance(Guid accountID);
        Task<Balance1Model> GetBalance(Guid accountID);
        Task<ICollection<Transaction1Model>> GetTransactions(Guid accountID);
    }
}
