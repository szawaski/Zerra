using Zerra.CQRS;

namespace Store.Orders.Domain.Commands
{
    public sealed class CancelOrderCommand : ICommand
    {
        public required Guid OrderID { get; set; }
    }
}
