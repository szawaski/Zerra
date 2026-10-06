using Zerra.CQRS;

namespace Store.Orders.Domain.Inventory
{
    /// <summary>
    /// Service-to-service commands used by the Orders service. The web gateway doesn't register this interface, so browsers can't send them.
    /// </summary>
    public interface IStockReservationHandler :
        ICommandHandler<ReserveStockCommand>,
        ICommandHandler<ReleaseReservedStockCommand>
    {
    }
}
