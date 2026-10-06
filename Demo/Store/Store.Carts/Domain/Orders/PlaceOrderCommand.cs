using Zerra.CQRS;

namespace Store.Carts.Domain.Orders
{
    public sealed class PlaceOrderCommand : ICommand<PlaceOrderResult>
    {
        public required Guid CustomerID { get; set; }
        public required OrderItemRequest[] Items { get; set; }
    }
}
