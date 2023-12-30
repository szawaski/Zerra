using System;
using System.Threading.Tasks;
using Zerra;
using Zerra.Repository;
using Zerra.Threading;
using ZerraDemo.Domain.Ledger1.Command;
using ZerraDemo.Domain.Ledger1.DataModels;
using ZerraDemo.Common;

namespace ZerraDemo.Domain.Ledger1
{
    public class Ledger1CommandHandler : ILedger1CommandHandler
    {
        private const string lockPurpose = "Transaction";

        public async Task Handle(Deposit1Command command)
        {
            Access.CheckRole("Admin");

            if (command.Amount < 0)
                throw new ArgumentException("Invalid Amount");

            using (await Locker<Guid>.LockAsync(lockPurpose, command.AccountID))
            {
                decimal currentBalance;
                var account = await Repo.QueryAsync(new QuerySingle<Ledger1AccountDataModel>(x => x.AccountID == command.AccountID));
                var hasBalance = account?.HasBalance == true;
                if (hasBalance)
                {
                    var current = await Repo.QueryAsync(new QuerySingle<Ledger1DataModel>(x => x.AccountID == command.AccountID));
                    if (current == null)
                        throw new Exception("Ledger not found");
                    currentBalance = current.Balance;
                }
                else
                {
                    currentBalance = 0;
                }
                var item = new Ledger1DataModel()
                {
                    AccountID = command.AccountID,
                    UpdatedDate = DateTime.UtcNow,
                    Balance = currentBalance + command.Amount //Deposit/Withdraw
                };

                if (!hasBalance)
                {
                    await Repo.PersistAsync(new Create<Ledger1DataModel>("Initial Deposit", command, item));
                    if (account == null)
                        Repo.Persist(new Create<Ledger1AccountDataModel>(new Ledger1AccountDataModel() { AccountID = command.AccountID, HasBalance = true }));
                    else
                        Repo.Persist(new Update<Ledger1AccountDataModel>(new Ledger1AccountDataModel() { AccountID = command.AccountID, HasBalance = true }));
                }
                else
                {
                    await Repo.PersistAsync(new Update<Ledger1DataModel>("Deposit", command, item, new Graph<Ledger1DataModel>(
                        x => x.UpdatedDate,
                        x => x.Balance
                    )));
                }
            }
        }

        public async Task Handle(Withdraw1Command command)
        {
            Access.CheckRole("Admin");

            if (command.Amount < 0)
                throw new ArgumentException("Invalid Amount");

            using (await Locker<Guid>.LockAsync(lockPurpose, command.AccountID))
            {
                decimal currentBalance;
                var account = await Repo.QueryAsync(new QuerySingle<Ledger1AccountDataModel>(x => x.AccountID == command.AccountID));
                var hasBalance = account?.HasBalance == true;
                if (hasBalance)
                {
                    var current = await Repo.QueryAsync(new QuerySingle<Ledger1DataModel>(x => x.AccountID == command.AccountID));
                    if (current == null)
                        throw new Exception("Ledger not found");
                    currentBalance = current.Balance;
                }
                else
                {
                    currentBalance = 0;
                }

                var item = new Ledger1DataModel()
                {
                    AccountID = command.AccountID,
                    UpdatedDate = DateTime.UtcNow,
                    Balance = currentBalance - command.Amount, //Deposit/Withdraw
                };

                if (!hasBalance)
                {
                    await Repo.PersistAsync(new Create<Ledger1DataModel>("Initial Withdraw", command, item));
                    if (account == null)
                        Repo.Persist(new Create<Ledger1AccountDataModel>(new Ledger1AccountDataModel() { AccountID = command.AccountID, HasBalance = true }));
                    else
                        Repo.Persist(new Update<Ledger1AccountDataModel>(new Ledger1AccountDataModel() { AccountID = command.AccountID, HasBalance = true }));
                }
                else
                {
                    await Repo.PersistAsync(new Update<Ledger1DataModel>("Withdraw", command, item, new Graph<Ledger1DataModel>(
                        x => x.UpdatedDate,
                        x => x.Balance
                    )));
                }
            }
        }

        public async Task Handle(Transfer1Command command)
        {
            Access.CheckRole("Admin");

            if (command.Amount < 0)
                throw new ArgumentException("Invalid Amount");

            using (await Locker<Guid>.LockAsync(lockPurpose, command.FromAccountID))
            using (await Locker<Guid>.LockAsync(lockPurpose, command.ToAccountID))
            {
                decimal fromCurrentBalance;
                var fromAccount = await Repo.QueryAsync(new QuerySingle<Ledger1AccountDataModel>(x => x.AccountID == command.FromAccountID));
                if (fromAccount == null)
                    throw new Exception("Account not found");
                var fromHasBalance = fromAccount.HasBalance == true;
                if (fromHasBalance)
                {
                    var current = await Repo.QueryAsync(new QuerySingle<Ledger1DataModel>(x => x.AccountID == command.FromAccountID));
                    if (current == null)
                        throw new Exception("Ledger not found");
                    fromCurrentBalance = current.Balance;
                }
                else
                {
                    fromCurrentBalance = 0;
                }

                decimal toCurrentBalance;
                var toAccount = await Repo.QueryAsync(new QuerySingle<Ledger1AccountDataModel>(x => x.AccountID == command.ToAccountID));
                if (toAccount == null)
                    throw new Exception("Account not found");
                var toHasBalance = fromAccount.HasBalance == true;
                if (toHasBalance)
                {
                    var current = await Repo.QueryAsync(new QuerySingle<Ledger1DataModel>(x => x.AccountID == command.ToAccountID));
                    if (current == null)
                        throw new Exception("Ledger not found");
                    toCurrentBalance = current.Balance;
                }
                else
                {
                    toCurrentBalance = 0;
                }

                var itemFrom = new Ledger1DataModel()
                {
                    AccountID = command.FromAccountID,
                    UpdatedDate = DateTime.UtcNow,
                    Balance = fromCurrentBalance - command.Amount
                };
                var itemTo = new Ledger1DataModel()
                {
                    AccountID = command.ToAccountID,
                    UpdatedDate = DateTime.UtcNow,
                    Balance = toCurrentBalance + command.Amount
                };

                if (!fromHasBalance)
                {
                    await Repo.PersistAsync(new Create<Ledger1DataModel>("Initial Transfer Withdraw", command, itemFrom));
                    if (fromAccount == null)
                        Repo.Persist(new Create<Ledger1AccountDataModel>(new Ledger1AccountDataModel() { AccountID = command.FromAccountID, HasBalance = true }));
                    else
                        Repo.Persist(new Update<Ledger1AccountDataModel>(new Ledger1AccountDataModel() { AccountID = command.FromAccountID, HasBalance = true }));
                }
                else
                {
                    await Repo.PersistAsync(new Update<Ledger1DataModel>("Transfer Withdraw", command, itemFrom, new Graph<Ledger1DataModel>(
                        x => x.UpdatedDate,
                        x => x.Balance
                    )));
                }

                if (!toHasBalance)
                {
                    await Repo.PersistAsync(new Create<Ledger1DataModel>("Initial Transfer Deposit", command, itemTo));
                    if (toAccount == null)
                        Repo.Persist(new Create<Ledger1AccountDataModel>(new Ledger1AccountDataModel() { AccountID = command.ToAccountID, HasBalance = true }));
                    else
                        Repo.Persist(new Update<Ledger1AccountDataModel>(new Ledger1AccountDataModel() { AccountID = command.ToAccountID, HasBalance = true }));
                }
                else
                {
                    await Repo.PersistAsync(new Update<Ledger1DataModel>("Transfer Deposit", command, itemTo, new Graph<Ledger1DataModel>(
                        x => x.UpdatedDate,
                        x => x.Balance
                    )));
                }
            }
        }
    }
}