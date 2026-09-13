using Store.Common;
using Store.Inventory.Domain;
using Store.Inventory.Domain.Commands;
using Store.Inventory.Service.Data;
using Zerra.Repository;

namespace Store.Inventory.Service.Handlers
{
    public sealed class InventoryCommandHandler : BaseHandlerWithRepo, IInventoryCommandHandler, IStockReservationHandler
    {
        private const int maxRestockQuantity = 10_000;

        /// <summary>
        /// Stock changes run one at a time so two orders can't reserve the same units, <see cref="OrderEventHandler"/> takes it too.
        /// That's enough for a single instance, a scaled out service would use optimistic concurrency in the data store instead.
        /// </summary>
        public static readonly SemaphoreSlim StockGate = new(1, 1);

        public async Task Handle(RestockProductCommand command, CancellationToken cancellationToken)
        {
            if (command.Quantity <= 0 || command.Quantity > maxRestockQuantity)
                throw new DomainException($"Restock quantity must be between 1 and {maxRestockQuantity:N0}.");

            await StockGate.WaitAsync(cancellationToken);
            try
            {
                //a product added to the catalog has no stock record until it's first restocked
                var item = await Repo.SingleAsync<StockItemDataModel>(x => x.ProductID == command.ProductID);
                if (item is null)
                {
                    item = new StockItemDataModel() { ProductID = command.ProductID, OnHand = command.Quantity };
                    await Repo.CreateAsync(item);
                }
                else
                {
                    item.OnHand += command.Quantity;
                    await Repo.UpdateAsync(item);
                }

                await Repo.CreateAsync(new StockMovementDataModel()
                {
                    ID = Guid.NewGuid(),
                    ProductID = item.ProductID,
                    Kind = nameof(StockMovementKind.Restocked),
                    Quantity = command.Quantity,
                    OccurredOn = DateTime.UtcNow
                });

                Log?.Info($"Restocked {command.Quantity} of product {item.ProductID}, {item.OnHand - item.Reserved} available");
            }
            finally
            {
                _ = StockGate.Release();
            }
        }

        public async Task Handle(ReserveStockCommand command, CancellationToken cancellationToken)
        {
            if (command.Lines is null || command.Lines.Length == 0)
                throw new DomainException("There is nothing to reserve.");

            await StockGate.WaitAsync(cancellationToken);
            try
            {
                //a retried command must not reserve the same order twice
                if (await Repo.AnyAsync<StockReservationDataModel>(x => x.OrderID == command.OrderID))
                    return;

                var lines = command.Lines
                    .GroupBy(x => x.ProductID)
                    .Select(x => (ProductID: x.Key, ProductName: x.First().ProductName ?? "A product", Quantity: x.Sum(y => y.Quantity)))
                    .ToArray();

                var productIDs = lines.Select(x => x.ProductID).ToArray();
                var items = (await Repo.ManyAsync<StockItemDataModel>(x => productIDs.Contains(x.ProductID))).ToDictionary(x => x.ProductID);

                //every line is checked before anything is saved, so a short product fails the order without reserving the rest
                foreach (var line in lines)
                {
                    if (line.Quantity <= 0)
                        throw new DomainException("Reserved quantity must be at least 1.");

                    var available = items.TryGetValue(line.ProductID, out var item) ? item.OnHand - item.Reserved : 0;
                    if (available == 0)
                        throw new DomainException($"{line.ProductName} is out of stock.");
                    if (line.Quantity > available)
                        throw new DomainException($"Only {available} of {line.ProductName} available, {line.Quantity} requested.");

                    item!.Reserved += line.Quantity;
                }

                await Repo.UpdateAsync(items.Values.ToArray());
                await Repo.CreateAsync(lines.Select(x => new StockReservationDataModel()
                {
                    ID = Guid.NewGuid(),
                    OrderID = command.OrderID,
                    OrderNumber = command.OrderNumber,
                    ProductID = x.ProductID,
                    Quantity = x.Quantity
                }).ToArray());

                var now = DateTime.UtcNow;
                await Repo.CreateAsync(lines.Select(x => new StockMovementDataModel()
                {
                    ID = Guid.NewGuid(),
                    ProductID = x.ProductID,
                    Kind = nameof(StockMovementKind.Reserved),
                    Quantity = x.Quantity,
                    OrderNumber = command.OrderNumber,
                    OccurredOn = now
                }).ToArray());

                Log?.Info($"Reserved {lines.Sum(x => x.Quantity)} units for order {command.OrderNumber}");
            }
            finally
            {
                _ = StockGate.Release();
            }
        }
    }
}
