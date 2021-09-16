using System.Threading.Tasks;
using ZerraDemo.Domain.Ledger2.Events;

namespace ZerraDemo.Domain.Ledger2
{
    public class Ledger2EventProvider : ILedger2EventProvider
    {
        public async Task Handle(Deposit2Event @event)
        {
            //Handles events from aggregate but this domain doesn't do anything with them
        }

        public async Task Handle(Withdraw2Event @event)
        {
            //Handles events from aggregate but this domain doesn't do anything with them
        }

        public async Task Handle(Transfer2Event @event)
        {
            //Handles events from aggregate but this domain doesn't do anything with them
        }
    }
}