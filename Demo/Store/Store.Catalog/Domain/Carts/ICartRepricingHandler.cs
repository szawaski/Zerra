using Zerra.CQRS;

namespace Store.Catalog.Domain.Carts
{
    /// <summary>
    /// Service-to-service commands used by the Catalog service. The web gateway doesn't register this interface, so browsers can't send them.
    /// </summary>
    public interface ICartRepricingHandler :
        ICommandHandler<RepriceCartItemsCommand>
    {
    }
}
