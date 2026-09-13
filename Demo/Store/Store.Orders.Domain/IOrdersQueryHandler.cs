using Store.Orders.Domain.Models;
using Zerra.CQRS;

namespace Store.Orders.Domain
{
    public interface IOrdersQueryHandler : IQueryHandler
    {
        Task<string> GetDataStoreName(CancellationToken cancellationToken);

        Task<CustomerModel[]> GetCustomers(CancellationToken cancellationToken);
        Task<OrderModel[]> GetOrders(CancellationToken cancellationToken);
        Task<OrderModel> GetOrder(Guid orderID, CancellationToken cancellationToken);
    }
}
