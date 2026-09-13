using Store.Orders.Domain.Models;
using Zerra.CQRS;

namespace Store.Orders.Domain.Commands
{
    public sealed class PlaceOrderCommand : ICommand<PlaceOrderResult>
    {
        public required Guid CustomerID { get; set; }
        public required OrderItemRequest[] Items { get; set; }
    }
}
