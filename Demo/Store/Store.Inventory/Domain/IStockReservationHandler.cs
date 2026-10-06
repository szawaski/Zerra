using Store.Inventory.Domain.Commands;
using Zerra.CQRS;

namespace Store.Inventory.Domain
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
