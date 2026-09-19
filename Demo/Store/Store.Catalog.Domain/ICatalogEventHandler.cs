using Store.Catalog.Domain.Events;
using Zerra.CQRS;

namespace Store.Catalog.Domain
{
    /// <summary>
    /// The events the Catalog service publishes. A downstream service implements this interface to subscribe.
    /// </summary>
    /// <remarks>
    /// These are events and not commands because every replica of a subscriber needs its own copy: each one caches products
    /// separately, so each one has to drop its own stale copy. A command would reach one replica and leave the rest stale.
    /// Nothing here changes stored state, which is what makes running on every replica harmless.
    /// </remarks>
    public interface ICatalogEventHandler :
        IEventHandler<ProductPriceChangedEvent>,
        IEventHandler<ProductDiscontinuedEvent>
    {
    }
}
