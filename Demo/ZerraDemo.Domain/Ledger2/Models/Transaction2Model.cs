using System;

namespace ZerraDemo.Domain.Ledger2.Models
{
    public class Transaction2Model
    {
        public Guid AccountID { get; set; }
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
        public string? Description { get; set; }
        public decimal Balance { get; set; }
        public string? Event { get; set; }
    }
}
