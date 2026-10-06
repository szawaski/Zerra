using Store.Inventory.Domain.Commands;
using Zerra.CQRS;

namespace Store.Inventory.Domain
{
    /// <summary>
    /// Inventory commands for the storefront, routed through the web gateway.
    /// </summary>
    public interface IInventoryCommandHandler :
        ICommandHandler<RestockProductCommand>
    {
    }
}
