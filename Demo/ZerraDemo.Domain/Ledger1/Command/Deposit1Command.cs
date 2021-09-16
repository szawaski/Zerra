using System;
using Zerra.CQRS;

namespace ZerraDemo.Domain.Ledger1.Command
{
    [ServiceExposed]
    public class Deposit1Command : ICommand
    {
        public Guid AccountID { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; }
    }
}
