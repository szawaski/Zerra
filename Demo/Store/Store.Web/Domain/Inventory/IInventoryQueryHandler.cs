using Zerra.CQRS;

namespace Store.Web.Domain.Inventory
{
    public interface IInventoryQueryHandler : IQueryHandler
    {
        Task<string> GetDataStoreName(CancellationToken cancellationToken);
        Task<string> GetMessagingName(CancellationToken cancellationToken);

        Task<StockLevelModel[]> GetStockLevels(CancellationToken cancellationToken);
        Task<StockMovementModel[]> GetRecentMovements(int count, CancellationToken cancellationToken);
    }
}
