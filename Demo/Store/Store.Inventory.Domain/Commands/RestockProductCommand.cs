using Zerra.CQRS;

namespace Store.Inventory.Domain.Commands
{
    public sealed class RestockProductCommand : ICommand
    {
        public required Guid ProductID { get; set; }
        public required int Quantity { get; set; }
    }
}
