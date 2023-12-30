using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Zerra.Repository;
using ZerraDemo.Domain.Ledger1.Command;
using ZerraDemo.Domain.Ledger1.DataModels;
using ZerraDemo.Domain.Ledger1.Models;
using ZerraDemo.Common;

namespace ZerraDemo.Domain.Ledger1
{
    public class Ledger1QueryProvider : ILedger1QueryProvider
    {
        public async Task<bool> HasBalance(Guid accountID)
        {
            Access.CheckRole("Admin");

            var item = await Repo.QueryAsync(new QuerySingle<Ledger1AccountDataModel>(x => x.AccountID == accountID));
            if (item == null)
                return false;
            return item.HasBalance;
        }

        public async Task<Balance1Model> GetBalance(Guid accountID)
        {
            Access.CheckRole("Admin");

            var items = await Repo.QueryAsync(new TemporalQueryMany<Ledger1DataModel>((DateTime?)null, null, 0, 2, x => x.AccountID == accountID));
            var reversed = items.Reverse();
            var current = reversed.FirstOrDefault();

            if (current == null)
            {
                return new Balance1Model()
                {
                    AccountID = accountID,
                    Balance = 0,
                    LastTransactionAmount = null,
                    LastTransactionDate = null
                };
            }

            var previous = reversed.Skip(1).FirstOrDefault();

            var model = new Balance1Model()
            {
                AccountID = accountID,
                Balance = current.Balance,
                LastTransactionAmount = previous == null ? current.Balance : current.Balance - previous.Balance,
                LastTransactionDate = current.UpdatedDate
            };

            return model;
        }

        public async Task<ICollection<Transaction1Model>> GetTransactions(Guid accountID)
        {
            Access.CheckRole("Admin");

            var items = await Repo.QueryAsync(new EventQueryMany<Ledger1DataModel>((DateTime?)null, null, x => x.AccountID == accountID));

            var models = new List<Transaction1Model>();
            foreach (var item in items)
            {
                if (item.Source == null)
                    continue;

                decimal amount;
                string? description;
                switch (item.SourceType)
                {
                    case nameof(Deposit1Command):
                        {
                            var command = (Deposit1Command)item.Source;
                            amount = command.Amount;
                            description = command.Description;
                        }
                        break;
                    case nameof(Withdraw1Command):
                        {
                            var command = (Withdraw1Command)item.Source;
                            amount = -command.Amount;
                            description = command.Description;
                        }
                        break;
                    case nameof(Transfer1Command):
                        {
                            var command = (Transfer1Command)item.Source;
                            if (command.FromAccountID == accountID)
                                amount = -command.Amount;
                            else
                                amount = command.Amount;
                            description = command.Description;
                        }
                        break;
                    default:
                        throw new NotSupportedException($"Event source not recognized: {item.SourceType}");
                }

                var model = new Transaction1Model()
                {
                    AccountID = accountID,
                    Date = item.Date,
                    Amount = amount,
                    Description = description,
                    Balance = item.Model.Balance,
                    Event = item.EventName
                };
                models.Add(model);
            }

            return models;
        }
    }
}