using Store.Catalog.Domain;
using Store.Common;
using Store.Inventory.Domain.Commands;
using Store.Inventory.Domain.Models;
using Store.Orders.Domain;
using Store.Orders.Domain.Commands;
using Store.Orders.Domain.Events;
using Store.Orders.Domain.Models;
using Store.Orders.Service.Data;
using Zerra;
using Zerra.Repository;

namespace Store.Orders.Service.Handlers
{
    public sealed class OrdersCommandHandler : BaseHandlerWithRepo, IOrdersCommandHandler
    {
        private const int maxItems = 20;
        private const int maxQuantity = 100;

        public async Task<PlaceOrderResult> Handle(PlaceOrderCommand command, CancellationToken cancellationToken)
        {
            if (!await Repo.AnyAsync<CustomerDataModel>(x => x.ID == command.CustomerID))
                throw new DomainException("Customer not found.");
            if (command.Items is null || command.Items.Length == 0)
                throw new DomainException("An order needs at least one item.");

            var items = command.Items
                .GroupBy(x => x.ProductID)
                .Select(x => (ProductID: x.Key, Quantity: x.Sum(y => y.Quantity)))
                .ToArray();
            if (items.Length > maxItems)
                throw new DomainException($"An order can have at most {maxItems} different products.");

            //Query the Catalog service: names and prices come from the catalog, never from the browser
            var productIDs = items.Select(x => x.ProductID).ToArray();
            var products = (await Bus.Call<ICatalogQueryHandler>().GetProductsByIDs(productIDs, cancellationToken)).ToDictionary(x => x.ID);

            var placedOn = DateTime.UtcNow;
            var order = new OrderDataModel()
            {
                ID = Guid.NewGuid(),
                OrderNumber = $"SO-{placedOn:yyMMdd}-{Random.Shared.Next(1000, 10000)}",
                CustomerID = command.CustomerID,
                PlacedOn = placedOn,
                Status = nameof(OrderStatus.Placed)
            };

            //the name and price are a snapshot of the catalog when the order is placed
            var orderItems = new OrderItemDataModel[items.Length];
            for (var i = 0; i < items.Length; i++)
            {
                if (!products.TryGetValue(items[i].ProductID, out var product))
                    throw new DomainException("A product in the order no longer exists.");
                if (!product.IsActive)
                    throw new DomainException($"{product.Name} has been discontinued.");
                if (items[i].Quantity < 1 || items[i].Quantity > maxQuantity)
                    throw new DomainException($"Quantity of {product.Name} must be between 1 and {maxQuantity}.");

                orderItems[i] = new OrderItemDataModel()
                {
                    ID = Guid.NewGuid(),
                    OrderID = order.ID,
                    ProductID = product.ID,
                    ProductName = product.Name,
                    UnitPrice = product.Price,
                    Quantity = items[i].Quantity
                };
            }
            var total = orderItems.Sum(x => x.UnitPrice * x.Quantity);

            //Command the Inventory service and wait: if any product is short it throws and the order is never saved
            await Bus.DispatchAwaitAsync(new ReserveStockCommand()
            {
                OrderID = order.ID,
                OrderNumber = order.OrderNumber,
                Items = orderItems.Select(x => new StockReservationItem() { ProductID = x.ProductID, ProductName = x.ProductName, Quantity = x.Quantity }).ToArray()
            });

            try
            {
                await Repo.CreateAsync(order);
                await Repo.CreateAsync(orderItems);
            }
            catch
            {
                //compensate: the stock is already reserved, command Inventory to release it
                await Bus.DispatchAsync(new ReleaseReservedStockCommand() { OrderID = order.ID, OrderNumber = order.OrderNumber });
                throw;
            }

            Log?.Info($"Placed order {order.OrderNumber} for {total:0.00}");
            return new PlaceOrderResult() { OrderID = order.ID, OrderNumber = order.OrderNumber, Total = total };
        }

        public async Task Handle(CancelOrderCommand command, CancellationToken cancellationToken)
        {
            var order = await CloseOrderAsync(command.OrderID, OrderStatus.Cancelled, "cancelled");

            //Command Inventory to release the reserved stock. A command, not an event: it moves stock, so it must happen once
            //however many Inventory replicas are running.
            await Bus.DispatchAsync(new ReleaseReservedStockCommand() { OrderID = order.ID, OrderNumber = order.OrderNumber! });

            Log?.Info($"Cancelled order {order.OrderNumber}");
        }

        public async Task Handle(ShipOrderCommand command, CancellationToken cancellationToken)
        {
            var order = await CloseOrderAsync(command.OrderID, OrderStatus.Shipped, "shipped");

            //One event, announced once, and Orders doesn't name who acts on it. Inventory takes the reserved units off the shelf and
            //Shipping creates the shipment, and both write once, which normally rules an event out. Both subscribe with
            //EventConsumerMode.PerService, so their replicas compete for the event and one replica of each service handles it.
            await Bus.DispatchAsync(new OrderShippedEvent() { OrderID = order.ID, OrderNumber = order.OrderNumber!, ShippedOn = order.ClosedOn!.Value });

            Log?.Info($"Shipped order {order.OrderNumber}");
        }

        //an order is placed and then either ships or is cancelled, never both
        private async Task<OrderDataModel> CloseOrderAsync(Guid orderID, OrderStatus status, string action)
        {
            var order = await Repo.SingleAsync<OrderDataModel>(x => x.ID == orderID) ?? throw new DomainException("Order not found.");
            if (order.Status != nameof(OrderStatus.Placed))
                throw new DomainException($"Order {order.OrderNumber} is {order.Status?.ToLowerInvariant()}, only placed orders can be {action}.");

            order.Status = status.ToString();
            order.ClosedOn = DateTime.UtcNow;
            //the graph limits the update to the status columns
            await Repo.UpdateAsync(order, new Graph<OrderDataModel>(x => x.Status, x => x.ClosedOn));

            return order;
        }
    }
}
