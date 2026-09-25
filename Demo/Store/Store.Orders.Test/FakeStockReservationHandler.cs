using Store.Inventory.Domain;
using Store.Inventory.Domain.Commands;
using Zerra.CQRS;

namespace Store.Orders.Test
{
    public sealed class FakeStockReservationHandler : BaseHandler, IStockReservationHandler
    {
        public List<ReserveStockCommand> Reserved { get; } = new();
        public List<ReleaseReservedStockCommand> Released { get; } = new();
        /// <summary>When set, reserving fails with this, the way Inventory refuses an order it's short for.</summary>
        public Exception? ReserveFailure { get; set; }

        public Task Handle(ReserveStockCommand command, CancellationToken cancellationToken)
        {
            if (ReserveFailure is not null)
                return Task.FromException(ReserveFailure);
            Reserved.Add(command);
            return Task.CompletedTask;
        }

        public Task Handle(ReleaseReservedStockCommand command, CancellationToken cancellationToken)
        {
            Released.Add(command);
            return Task.CompletedTask;
        }
    }
}
