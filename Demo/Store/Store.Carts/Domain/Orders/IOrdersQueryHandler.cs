using Zerra.CQRS;

namespace Store.Carts.Domain.Orders
{
    public interface IOrdersQueryHandler : IQueryHandler
    {
        Task<CustomerModel[]> GetCustomers(CancellationToken cancellationToken);
    }
}
