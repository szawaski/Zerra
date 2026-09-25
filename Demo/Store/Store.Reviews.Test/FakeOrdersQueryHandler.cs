using Store.Orders.Domain;
using Store.Orders.Domain.Models;
using Zerra.CQRS;

namespace Store.Reviews.Test
{
    public sealed class FakeOrdersQueryHandler : BaseHandler, IOrdersQueryHandler
    {
        public List<CustomerModel> Customers { get; } = new();
        /// <summary>The customer and product pairs that were shipped, a verified purchase.</summary>
        public HashSet<(Guid CustomerID, Guid ProductID)> Purchases { get; } = new();

        public Task<CustomerModel[]> GetCustomers(CancellationToken cancellationToken) => Task.FromResult(Customers.ToArray());
        public Task<bool> HasPurchased(Guid customerID, Guid productID, CancellationToken cancellationToken) => Task.FromResult(Purchases.Contains((customerID, productID)));

        public Task<string> GetDataStoreName(CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<string> GetMessagingName(CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<OrderModel[]> GetOrders(CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<OrderModel> GetOrder(Guid orderID, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
