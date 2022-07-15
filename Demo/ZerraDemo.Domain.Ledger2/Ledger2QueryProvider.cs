using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZerraDemo.Domain.Ledger2.Aggregates;
using ZerraDemo.Domain.Ledger2.Models;
using ZerraDemo.Common;

namespace ZerraDemo.Domain.Ledger2
{
    public class Ledger2QueryProvider : ILedger2QueryProvider
    {
        public async Task<bool> HasBalance(Guid accountID)
        {
            Access.CheckRole("Admin");

            var aggregate = new Transaction2Aggregate(accountID);
            _ = await aggregate.RebuildOneEvent();
            return aggregate.LastEventNumber.HasValue;
        }

        public async Task<Balance2Model> GetBalance(Guid accountID)
        {
            Access.CheckRole("Admin");

            var aggregate = new Transaction2Aggregate(accountID);
            _ = await aggregate.Rebuild();
            var model = new Balance2Model()
            {
                AccountID = aggregate.ID,
                Balance = aggregate.Balance,
                LastTransactionAmount = aggregate.LastTransactionAmount,
                LastTransactionDate = aggregate.LastEventDate
            };
            return model;
        }

        public async Task<ICollection<Transaction2Model>> GetTransactions(Guid accountID)
        {
            Access.CheckRole("Admin");

            var aggregate = new Transaction2Aggregate(accountID);
            var models = new List<Transaction2Model>();
            while (await aggregate.RebuildOneEvent())
            {
                var model = new Transaction2Model()
                {
                    AccountID = aggregate.ID,
                    Balance = aggregate.Balance,
                    Amount = aggregate.LastTransactionAmount.Value,
                    Date = aggregate.LastEventDate.Value,
                    Description = aggregate.LastTransactionDescription,
                    Event = aggregate.LastEventName
                };
                models.Add(model);
            }
            return models;
        }
    }
}