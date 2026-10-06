using Zerra.CQRS;

namespace Store.Web.Domain.Inventory
{
    /// <summary>
    /// Inventory commands for the storefront, routed through the web gateway.
    /// </summary>
    public interface IInventoryCommandHandler :
        ICommandHandler<RestockProductCommand>
    {
    }
}
