using System;
using Zerra.Repository;

namespace ZerraDemo.Domain.Ledger1.DataModels
{
    [Entity("Ledger1Account")]
    public class Ledger1AccountDataModel
    {
        [Identity]
        public Guid AccountID { get; set; }
        public bool HasBalance { get; set; }
    }
}
