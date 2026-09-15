using Store.Common.Data;
using Store.Shipping.Domain;
using Store.Shipping.Domain.Models;
using Store.Shipping.Service.Data;
using Zerra;
using Zerra.Repository;

namespace Store.Shipping.Service.Handlers
{
    public sealed class ShippingQueryHandler : BaseHandlerWithRepo, IShippingQueryHandler
    {
        private const int recentShipmentCount = 50;

        public Task<string> GetDataStoreName(CancellationToken cancellationToken) => Task.FromResult(Context.GetService<IDataStoreInfo>().Description);

        public async Task<ShipmentModel[]> GetShipments(CancellationToken cancellationToken)
        {
            var items = await Repo.ManyAsync(QueryOrder<ShipmentDataModel>.Create(x => x.ShippedOn, true), 0, recentShipmentCount);
            return items.Select(x => new ShipmentModel()
            {
                OrderID = x.OrderID,
                OrderNumber = x.OrderNumber,
                Carrier = x.Carrier,
                TrackingNumber = x.TrackingNumber,
                Status = x.Status,
                ShippedOn = x.ShippedOn,
                DeliveredOn = x.DeliveredOn
            }).ToArray();
        }
    }
}
