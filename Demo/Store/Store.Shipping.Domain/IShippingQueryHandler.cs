using Store.Shipping.Domain.Models;
using Zerra.CQRS;

namespace Store.Shipping.Domain
{
    public interface IShippingQueryHandler : IQueryHandler
    {
        Task<string> GetDataStoreName(CancellationToken cancellationToken);

        Task<ShipmentModel[]> GetShipments(CancellationToken cancellationToken);
    }
}
