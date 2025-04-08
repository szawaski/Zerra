using System.Threading.Tasks;
using ZerraDemo.Domain.Ledger2.Aggregates;
using ZerraDemo.Domain.Ledger2.Command;
using ZerraDemo.Common;
using System.Threading;

namespace ZerraDemo.Domain.Ledger2
{
    public class Ledger2CommandHandler : ILedger2CommandHandler
    {
        public async Task Handle(Deposit2Command command, CancellationToken cancellationToken)
        {
            Access.CheckRole("Admin");

            var aggregate = new Transaction2Aggregate(command.AccountID);
            await aggregate.Deposit(command.Amount, command.Description);
        }

        public async Task Handle(Withdraw2Command command, CancellationToken cancellationToken)
        {
            Access.CheckRole("Admin");

            var aggregate = new Transaction2Aggregate(command.AccountID);
            await aggregate.Withdraw(command.Amount, command.Description);
        }

        public async Task Handle(Transfer2Command command, CancellationToken cancellationToken)
        {
            Access.CheckRole("Admin");

            var aggregateTo = new Transaction2Aggregate(command.ToAccountID);
            var aggregateFrom = new Transaction2Aggregate(command.FromAccountID);

            await aggregateFrom.TransferFrom(command.ToAccountID, command.Amount, command.Description);
            await aggregateTo.TransferTo(command.FromAccountID, command.Amount, command.Description);
        }
    }
}