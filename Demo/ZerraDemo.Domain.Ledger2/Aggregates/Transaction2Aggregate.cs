using System;
using System.Threading.Tasks;
using Zerra.Repository;
using ZerraDemo.Domain.Ledger2.Events;

namespace ZerraDemo.Domain.Ledger2.Aggregates
{
    public class Transaction2Aggregate : AggregateRoot
    {
        public Transaction2Aggregate(Guid id) : base(id) { }

        public decimal Balance { get; protected set; }
        public string LastTransactionDescription { get; protected set; }
        public decimal? LastTransactionAmount { get; protected set; }

        protected void Apply(Deposit2Event @event)
        {
            Balance += @event.Amount;
            LastTransactionDescription = @event.Description;
            LastTransactionAmount = @event.Amount;
        }

        protected void Apply(Withdraw2Event @event)
        {
            Balance -= @event.Amount;
            LastTransactionDescription = @event.Description;
            LastTransactionAmount = -@event.Amount;
        }

        protected void Apply(Transfer2Event @event)
        {
            Balance += @event.Amount;
            LastTransactionDescription = @event.Description;
            LastTransactionAmount = @event.Amount;
        }

        public async Task Deposit(decimal amount, string description)
        {
            if (amount < 0)
                throw new ArgumentException("Invalid Amount");

            _ = await Rebuild();

            var @event = new Deposit2Event()
            {
                AccountID = ID,
                Amount = amount,
                Description = description
            };
            await Append(@event);
        }

        public async Task Withdraw(decimal amount, string description)
        {
            if (amount < 0)
                throw new ArgumentException("Invalid Amount");

            _ = await Rebuild();

            if (Balance - amount < 0)
                throw new Exception("Insuffient Balance");

            var @event = new Withdraw2Event()
            {
                AccountID = ID,
                Amount = amount,
                Description = description
            };
            await Append(@event);
        }

        public async Task TransferFrom(Guid toAccountID, decimal amount, string description)
        {
            if (amount < 0)
                throw new ArgumentException("Invalid Amount");

            _ = await Rebuild();

            if (Balance - amount < 0)
                throw new Exception("Insuffient Balance");

            var @eventTo = new Transfer2Event()
            {
                AccountID = toAccountID,
                FromAccountID = ID,
                Amount = -amount,
                Description = description
            };
            await Append(@eventTo, true);
        }

        public async Task TransferTo(Guid fromAccountID, decimal amount, string description)
        {
            if (amount < 0)
                throw new ArgumentException("Invalid Amount");

            var @eventTo = new Transfer2Event()
            {
                AccountID = ID,
                FromAccountID = fromAccountID,
                Amount = amount,
                Description = description
            };
            await Append(@eventTo, true);
        }
    }
}
