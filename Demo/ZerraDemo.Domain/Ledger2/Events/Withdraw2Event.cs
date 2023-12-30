using System;
using Zerra.CQRS;

namespace ZerraDemo.Domain.Ledger2.Events
{
    public class Withdraw2Event : IEvent
    {
        public Guid AccountID { get; set; }
        public decimal Amount { get; set; }
        public string? Description { get; set; }
    }
}
