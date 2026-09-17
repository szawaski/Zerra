using Zerra.CQRS;

namespace Store.Carts.Domain.Commands
{
    public sealed class EmptyCartCommand : ICommand
    {
        public required Guid CustomerID { get; set; }
    }
}
