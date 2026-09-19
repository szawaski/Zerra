using Store.Carts.Service.Data;
using Store.Catalog.Domain;
using Store.Catalog.Domain.Events;
using Zerra.CQRS;

namespace Store.Carts.Service.Handlers
{
    /// <summary>
    /// Subscribes to the Catalog service, and only drops this instance's cached copy of the product. Nothing here writes to the event store.
    /// </summary>
    /// <remarks>
    /// This is the right shape for an event handler: the bus fans an event out to every replica, so the work has to be something that is
    /// correct when every replica does it. Dropping a local cache entry is. Repricing the carts is not, which is why the Catalog sends
    /// <see cref="Store.Carts.Domain.Commands.RepriceCartItemsCommand"/> for that, and one replica handles it.
    /// </remarks>
    public sealed class CatalogEventHandler : BaseHandler, ICatalogEventHandler
    {
        public Task Handle(ProductPriceChangedEvent @event)
        {
            if (Context.GetService<ICatalogProductCache>().Drop(@event.ProductID))
                Log?.Info($"Event: {@event.Name} is now {@event.NewPrice:0.00}, dropped from this instance's product cache");
            return Task.CompletedTask;
        }

        public Task Handle(ProductDiscontinuedEvent @event)
        {
            if (Context.GetService<ICatalogProductCache>().Drop(@event.ProductID))
                Log?.Info($"Event: {@event.Name} was discontinued, dropped from this instance's product cache");
            return Task.CompletedTask;
        }
    }
}
