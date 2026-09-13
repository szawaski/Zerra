using Store.Inventory.Domain.Models;
using Zerra.CQRS;

namespace Store.Inventory.Domain
{
    public interface IInventoryQueryHandler : IQueryHandler
    {
        Task<string> GetDataStoreName(CancellationToken cancellationToken);

        Task<StockLevelModel[]> GetStockLevels(CancellationToken cancellationToken);
        Task<StockMovementModel[]> GetRecentMovements(int count, CancellationToken cancellationToken);
    }
}
