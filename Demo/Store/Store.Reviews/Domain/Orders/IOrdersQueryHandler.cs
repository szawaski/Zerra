using Zerra.CQRS;

namespace Store.Reviews.Domain.Orders
{
    public interface IOrdersQueryHandler : IQueryHandler
    {
        Task<CustomerModel[]> GetCustomers(CancellationToken cancellationToken);
        Task<bool> HasPurchased(Guid customerID, Guid productID, CancellationToken cancellationToken);
    }
}
