using System;

namespace ZerraDemo.Domain.Ledger1.Models
{
    public class Balance1Model
    {
        public Guid AccountID { get; set; }
        public decimal Balance { get; set; }

        public DateTime? LastTransactionDate { get; set; }
        public decimal? LastTransactionAmount { get; set; }
    }
}
