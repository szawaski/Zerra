using System.Linq.Expressions;
using Store.Common;
using Store.Common.Data;
using Store.Common.Messaging;
using Store.Orders.Domain;
using Store.Orders.Domain.Models;
using Store.Orders.Service.Data;
using Zerra;
using Zerra.Repository;

namespace Store.Orders.Service.Handlers
{
    public sealed class OrdersQueryHandler : BaseHandlerWithRepo, IOrdersQueryHandler
    {
        private const int recentOrderCount = 50;

        public Task<string> GetDataStoreName(CancellationToken cancellationToken) => Task.FromResult(Context.GetService<IDataStoreInfo>().Description);
        public Task<string> GetMessagingName(CancellationToken cancellationToken) => Task.FromResult(Context.GetService<IMessagingInfo>().Description);

        public async Task<CustomerModel[]> GetCustomers(CancellationToken cancellationToken)
        {
            var items = await Repo.Query<CustomerDataModel>().OrderBy(x => x.Name).ToArrayAsync(cancellationToken);
            return items.Select(x => new CustomerModel() { ID = x.ID, Name = x.Name, Email = x.Email }).ToArray();
        }

        public async Task<OrderModel[]> GetOrders(CancellationToken cancellationToken)
        {
            var orders = await Repo.Query<OrderDataModel>(WithRelations()).OrderByDescending(x => x.PlacedOn).Take(recentOrderCount).ToArrayAsync(cancellationToken);
            return ToModels(orders);
        }

        public async Task<OrderModel> GetOrder(Guid orderID, CancellationToken cancellationToken)
        {
            var order = await Repo.Query<OrderDataModel>(WithRelations()).Where(x => x.ID == orderID).SingleOrDefaultAsync(cancellationToken);
            if (order is null)
                throw new DomainException("Order not found.");
            return ToModels([order])[0];
        }

        public async Task<bool> HasPurchased(Guid customerID, Guid productID, CancellationToken cancellationToken)
        {
            var shippedOrderIDs = (await Repo.Query<OrderDataModel>().Where(x => x.CustomerID == customerID && x.Status == nameof(OrderStatus.Shipped)).ToArrayAsync(cancellationToken))
                .Select(x => x.ID)
                .ToArray();
            if (shippedOrderIDs.Length == 0)
                return false;
            return await Repo.Query<OrderItemDataModel>().AnyAsync(x => shippedOrderIDs.Contains(x.OrderID) && x.ProductID == productID, cancellationToken);
        }

        //all order columns plus the related customer for its name and the items, the items of every order load in one query
        private static Graph<OrderDataModel> WithRelations() => new(true, x => x.Customer, x => x.Items);

        private static OrderModel[] ToModels(IReadOnlyCollection<OrderDataModel> orders)
        {
            return orders.Select(order =>
            {
                var orderItems = order.Items!
                    .OrderBy(x => x.ProductName)
                    .Select(x => new OrderItemModel()
                    {
                        ProductID = x.ProductID,
                        ProductName = x.ProductName,
                        UnitPrice = x.UnitPrice,
                        Quantity = x.Quantity,
                        Total = x.UnitPrice * x.Quantity
                    })
                    .ToArray();

                return new OrderModel()
                {
                    ID = order.ID,
                    OrderNumber = order.OrderNumber,
                    CustomerID = order.CustomerID,
                    CustomerName = order.Customer?.Name,
                    PlacedOn = order.PlacedOn,
                    Status = order.Status,
                    Total = orderItems.Sum(x => x.Total),
                    Items = orderItems
                };
            }).ToArray();
        }
    }
}
