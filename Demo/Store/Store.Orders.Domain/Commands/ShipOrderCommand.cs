using Zerra.CQRS;

namespace Store.Orders.Domain.Commands
{
    public sealed class ShipOrderCommand : ICommand
    {
        public required Guid OrderID { get; set; }
    }
}
