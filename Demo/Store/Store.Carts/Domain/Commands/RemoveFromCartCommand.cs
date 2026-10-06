using Zerra.CQRS;

namespace Store.Carts.Domain.Commands
{
    public sealed class RemoveFromCartCommand : ICommand
    {
        public required Guid CustomerID { get; set; }
        public required Guid ProductID { get; set; }
    }
}
