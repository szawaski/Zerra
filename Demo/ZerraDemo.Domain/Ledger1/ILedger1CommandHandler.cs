using Zerra.CQRS;
using Zerra.Providers;
using ZerraDemo.Domain.Ledger1.Command;

namespace ZerraDemo.Domain.Ledger1
{
    public interface ILedger1CommandHandler :
        ICommandHandler<Deposit1Command>,
        ICommandHandler<Withdraw1Command>,
        ICommandHandler<Transfer1Command>
    {
    }
}
