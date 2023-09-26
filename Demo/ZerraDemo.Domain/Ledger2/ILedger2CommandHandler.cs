using Zerra.CQRS;
using ZerraDemo.Domain.Ledger2.Command;

namespace ZerraDemo.Domain.Ledger2
{
    public interface ILedger2CommandHandler :
        ICommandHandler<Deposit2Command>,
        ICommandHandler<Withdraw2Command>,
        ICommandHandler<Transfer2Command>
    {
    }
}
