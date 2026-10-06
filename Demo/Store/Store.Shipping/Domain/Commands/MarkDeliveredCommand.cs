using Zerra.CQRS;

namespace Store.Shipping.Domain.Commands
{
    public sealed class MarkDeliveredCommand : ICommand
    {
        public required Guid OrderID { get; set; }
    }
}
