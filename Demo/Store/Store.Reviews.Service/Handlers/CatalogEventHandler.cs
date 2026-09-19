using Store.Catalog.Domain;
using Store.Catalog.Domain.Events;
using Store.Reviews.Service.Data;
using Zerra.CQRS;

namespace Store.Reviews.Service.Handlers
{
    /// <summary>
    /// Subscribes to the Catalog service, the second subscriber to the same events alongside the Carts service. Both only drop their own
    /// instance's cached copy of the product, which is work that is correct for every replica of both services to do.
    /// </summary>
    public sealed class CatalogEventHandler : BaseHandler, ICatalogEventHandler
    {
        public Task Handle(ProductPriceChangedEvent @event)
        {
            if (Context.GetService<ICatalogProductCache>().Drop(@event.ProductID))
                Log?.Info($"Event: {@event.Name} changed price, dropped from this instance's product cache");
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
