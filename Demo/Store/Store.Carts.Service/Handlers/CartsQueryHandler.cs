using Store.Carts.Domain;
using Store.Carts.Domain.Models;
using Store.Carts.Service.Aggregates;
using Store.Common.Data;
using Store.Common.Messaging;
using Zerra.CQRS;
using Zerra.Repository;

namespace Store.Carts.Service.Handlers
{
    public sealed class CartsQueryHandler : BaseHandler, ICartsQueryHandler
    {
        private const int historyCount = 20;

        public Task<string> GetDataStoreName(CancellationToken cancellationToken) => Task.FromResult(Context.GetService<IDataStoreInfo>().Description);
        public Task<string> GetMessagingName(CancellationToken cancellationToken) => Task.FromResult(Context.GetService<IMessagingInfo>().Description);

        public async Task<CartModel> GetCart(Guid customerID, CancellationToken cancellationToken)
        {
            var cart = new CartAggregate(customerID, Context.GetService<IEventStoreEngine>());
            _ = await cart.Rebuild();

            return new CartModel()
            {
                CustomerID = customerID,
                Items = cart.Items.Select(x => new CartItemModel()
                {
                    ProductID = x.ProductID,
                    ProductName = x.ProductName,
                    UnitPrice = x.UnitPrice,
                    Quantity = x.Quantity,
                    Total = x.UnitPrice * x.Quantity
                }).ToArray(),
                ItemCount = cart.ItemCount,
                Total = cart.Total,
                LastEventNumber = cart.LastEventNumber,
                UpdatedOn = cart.LastEventDate,
                LastOrderNumber = cart.LastOrderNumber
            };
        }

        public async Task<CartHistoryModel[]> GetCartHistory(Guid customerID, CancellationToken cancellationToken)
        {
            var eventStore = Context.GetService<IEventStoreEngine>();

            //a full rebuild finds the number of the last event
            var current = new CartAggregate(customerID, eventStore);
            if (!await current.Rebuild())
                return [];
            var lastEventNumber = current.LastEventNumber!.Value;

            //a second cart is rebuilt only up to the event before the ones to show, then replayed one event at a time to capture the cart after each
            var replay = new CartAggregate(customerID, eventStore);
            if (lastEventNumber >= historyCount)
                _ = await replay.Rebuild(lastEventNumber - historyCount);

            var history = new List<CartHistoryModel>();
            while (await replay.RebuildOneEvent())
            {
                history.Add(new CartHistoryModel()
                {
                    EventNumber = replay.LastEventNumber!.Value,
                    EventName = replay.LastEventName,
                    OccurredOn = replay.LastEventDate!.Value,
                    ItemCount = replay.ItemCount,
                    Total = replay.Total
                });
            }

            history.Reverse();
            return history.ToArray();
        }
    }
}
