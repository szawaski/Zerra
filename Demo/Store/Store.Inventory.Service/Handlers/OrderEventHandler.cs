using Store.Inventory.Service.Data;
using Store.Orders.Domain;
using Store.Orders.Domain.Events;
using Zerra.Repository;

namespace Store.Inventory.Service.Handlers
{
    /// <summary>
    /// Subscribes to the Orders service's events. Reserved stock is released when an order is cancelled and leaves the shelf when it ships.
    /// </summary>
    public sealed class OrderEventHandler : BaseHandlerWithRepo, IOrderEventHandler
    {
        public Task Handle(OrderCancelledEvent @event)
            => SettleReservationsAsync(@event.OrderID, @event.OrderNumber, StockMovementKind.Released);

        public Task Handle(OrderShippedEvent @event)
            => SettleReservationsAsync(@event.OrderID, @event.OrderNumber, StockMovementKind.Shipped);

        private async Task SettleReservationsAsync(Guid orderID, string orderNumber, StockMovementKind kind)
        {
            await InventoryCommandHandler.StockGate.WaitAsync();
            try
            {
                //events can be delivered more than once, once the reservations are settled there's nothing left to do
                var reservations = await Repo.ManyAsync<StockReservationDataModel>(x => x.OrderID == orderID);
                if (reservations.Count == 0)
                {
                    Log?.Info($"No reservations left for order {orderNumber}, nothing to {(kind == StockMovementKind.Shipped ? "ship" : "release")}");
                    return;
                }

                var productIDs = reservations.Select(x => x.ProductID).Distinct().ToArray();
                var items = (await Repo.ManyAsync<StockItemDataModel>(x => productIDs.Contains(x.ProductID))).ToDictionary(x => x.ProductID);
                foreach (var reservation in reservations)
                {
                    var item = items[reservation.ProductID];
                    if (kind == StockMovementKind.Shipped)
                    {
                        //shipped units leave the shelf
                        item.Reserved -= reservation.Quantity;
                        item.OnHand -= reservation.Quantity;
                    }
                    else
                    {
                        item.Reserved = Math.Max(0, item.Reserved - reservation.Quantity);
                    }
                }

                await Repo.UpdateAsync(items.Values.ToArray());
                await Repo.DeleteAsync(reservations);

                var now = DateTime.UtcNow;
                await Repo.CreateAsync(reservations.Select(x => new StockMovementDataModel()
                {
                    ID = Guid.NewGuid(),
                    ProductID = x.ProductID,
                    Kind = kind.ToString(),
                    Quantity = x.Quantity,
                    OrderNumber = orderNumber,
                    OccurredOn = now
                }).ToArray());

                Log?.Info($"{kind} {reservations.Sum(x => x.Quantity)} units for order {orderNumber}");
            }
            finally
            {
                _ = InventoryCommandHandler.StockGate.Release();
            }
        }
    }
}
