using System;

namespace ZerraDemo.Domain.Ledger1.Models
{
    public class Transaction1Model
    {
        public Guid AccountID { get; set; }
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; }
        public decimal Balance { get; set; }
        public string Event { get; set; }
    }
}
