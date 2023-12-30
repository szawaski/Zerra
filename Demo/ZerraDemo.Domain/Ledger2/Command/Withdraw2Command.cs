using System;
using Zerra.CQRS;

namespace ZerraDemo.Domain.Ledger2.Command
{
    [ServiceExposed]
    public class Withdraw2Command : ICommand
    {
        public Guid AccountID { get; set; }
        public decimal Amount { get; set; }
        public string? Description { get; set; }
    }
}
