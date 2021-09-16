using Zerra.CQRS;
using Zerra.Providers;
using ZerraDemo.Domain.Ledger2.Events;

namespace ZerraDemo.Domain.Ledger2
{
    public interface ILedger2EventProvider : IBaseProvider,
        IEventHandler<Deposit2Event>,
        IEventHandler<Withdraw2Event>,
        IEventHandler<Transfer2Event>
    {
    }
}
