using Zerra.CQRS;

namespace Store.Web.Domain.Orders
{
    public interface IOrdersQueryHandler : IQueryHandler
    {
        Task<string> GetDataStoreName(CancellationToken cancellationToken);
        Task<string> GetMessagingName(CancellationToken cancellationToken);

        Task<CustomerModel[]> GetCustomers(CancellationToken cancellationToken);
        Task<OrderModel[]> GetOrders(CancellationToken cancellationToken);
        Task<OrderModel> GetOrder(Guid orderID, CancellationToken cancellationToken);

        /// <summary>
        /// True if the customer has a shipped order containing the product. Called by the Reviews service to mark a review as a verified purchase.
        /// </summary>
        Task<bool> HasPurchased(Guid customerID, Guid productID, CancellationToken cancellationToken);
    }
}
