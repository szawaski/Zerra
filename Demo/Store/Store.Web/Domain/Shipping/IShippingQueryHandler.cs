using Zerra.CQRS;

namespace Store.Web.Domain.Shipping
{
    public interface IShippingQueryHandler : IQueryHandler
    {
        Task<string> GetDataStoreName(CancellationToken cancellationToken);
        Task<string> GetMessagingName(CancellationToken cancellationToken);

        Task<ShipmentModel[]> GetShipments(CancellationToken cancellationToken);
    }
}
