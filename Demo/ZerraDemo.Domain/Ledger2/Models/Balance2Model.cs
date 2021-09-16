using System;

namespace ZerraDemo.Domain.Ledger2.Models
{
    public class Balance2Model
    {
        public Guid AccountID { get; set; }
        public decimal Balance { get; set; }

        public DateTime? LastTransactionDate { get; set; }
        public decimal? LastTransactionAmount { get; set; }
    }
}
