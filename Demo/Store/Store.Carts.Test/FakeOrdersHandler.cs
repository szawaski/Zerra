using Store.Orders.Domain;
using Store.Orders.Domain.Commands;
using Store.Orders.Domain.Models;
using Zerra.CQRS;

namespace Store.Carts.Test
{
    public sealed class FakeOrdersHandler : BaseHandler, IOrdersQueryHandler, IOrdersCommandHandler
    {
        public List<CustomerModel> Customers { get; } = new();
        public List<PlaceOrderCommand> PlacedOrders { get; } = new();
        /// <summary>When set, placing an order fails with this, the way Orders refuses an order it can't reserve stock for.</summary>
        public Exception? PlaceOrderFailure { get; set; }

        public Task<CustomerModel[]> GetCustomers(CancellationToken cancellationToken) => Task.FromResult(Customers.ToArray());

        public Task<PlaceOrderResult> Handle(PlaceOrderCommand command, CancellationToken cancellationToken)
        {
            if (PlaceOrderFailure is not null)
                return Task.FromException<PlaceOrderResult>(PlaceOrderFailure);
            PlacedOrders.Add(command);
            return Task.FromResult(new PlaceOrderResult() { OrderID = Guid.NewGuid(), OrderNumber = $"SO-TEST-{PlacedOrders.Count}", Total = 100m * PlacedOrders.Count });
        }

        public Task<string> GetDataStoreName(CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<string> GetMessagingName(CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<OrderModel[]> GetOrders(CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<OrderModel> GetOrder(Guid orderID, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> HasPurchased(Guid customerID, Guid productID, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task Handle(CancelOrderCommand command, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task Handle(ShipOrderCommand command, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
