using Store.Common.Data;
using Store.Inventory.Domain;
using Store.Inventory.Domain.Models;
using Store.Inventory.Service.Data;
using Zerra.Repository;

namespace Store.Inventory.Service.Handlers
{
    public sealed class InventoryQueryHandler : BaseHandlerWithRepo, IInventoryQueryHandler
    {
        public Task<string> GetDataStoreName(CancellationToken cancellationToken) => Task.FromResult(Context.GetService<IDataStoreInfo>().Description);

        public async Task<StockLevelModel[]> GetStockLevels(CancellationToken cancellationToken)
        {
            var items = await Repo.ManyAsync<StockItemDataModel>();
            return items.Select(x => new StockLevelModel()
            {
                ProductID = x.ProductID,
                OnHand = x.OnHand,
                Reserved = x.Reserved,
                Available = x.OnHand - x.Reserved
            }).ToArray();
        }

        public async Task<StockMovementModel[]> GetRecentMovements(int count, CancellationToken cancellationToken)
        {
            count = Math.Clamp(count, 1, 100);
            var items = await Repo.ManyAsync<StockMovementDataModel>(QueryOrder<StockMovementDataModel>.Create(x => x.OccurredOn, true), 0, count);
            return items.Select(x => new StockMovementModel()
            {
                ProductID = x.ProductID,
                Kind = x.Kind,
                Quantity = x.Quantity,
                OrderNumber = x.OrderNumber,
                OccurredOn = x.OccurredOn
            }).ToArray();
        }
    }
}
