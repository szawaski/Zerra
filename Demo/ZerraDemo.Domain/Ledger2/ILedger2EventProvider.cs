using Zerra.CQRS;
using ZerraDemo.Domain.Ledger2.Events;

namespace ZerraDemo.Domain.Ledger2
{
    public interface ILedger2EventProvider :
        IEventHandler<Deposit2Event>,
        IEventHandler<Withdraw2Event>,
        IEventHandler<Transfer2Event>
    {
    }
}
