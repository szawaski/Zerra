using System;
using Zerra.Repository;

namespace ZerraDemo.Domain.Ledger1.DataModels
{
    [Entity("Ledger1")]
    public class Ledger1DataModel
    {
        [Identity]
        public Guid AccountID { get; set; }
        public DateTime UpdatedDate { get; set; }
        public decimal Balance { get; set; }
    }
}
