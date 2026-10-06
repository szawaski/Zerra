using Zerra.CQRS;

namespace Store.Web.Domain.Orders
{
    public sealed class ShipOrderCommand : ICommand
    {
        public required Guid OrderID { get; set; }
    }
}
