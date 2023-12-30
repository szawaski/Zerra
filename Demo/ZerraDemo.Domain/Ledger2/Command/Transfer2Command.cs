using System;
using Zerra.CQRS;

namespace ZerraDemo.Domain.Ledger2.Command
{
    [ServiceExposed]
    public class Transfer2Command : ICommand
    {
        public Guid FromAccountID { get; set; }
        public Guid ToAccountID { get; set; }
        public decimal Amount { get; set; }
        public string? Description { get; set; }
    }
}
