using Store.Catalog.Domain;
using Store.Catalog.Domain.Events;
using Zerra.CQRS;

namespace Store.Catalog.Test
{
    public sealed class FakeCatalogEventHandler : BaseHandler, ICatalogEventHandler
    {
        public List<ProductPriceChangedEvent> PriceChanges { get; } = new();
        public List<ProductDiscontinuedEvent> Discontinued { get; } = new();

        public Task Handle(ProductPriceChangedEvent @event)
        {
            PriceChanges.Add(@event);
            return Task.CompletedTask;
        }

        public Task Handle(ProductDiscontinuedEvent @event)
        {
            Discontinued.Add(@event);
            return Task.CompletedTask;
        }
    }
}
