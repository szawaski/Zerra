using Zerra.CQRS;

namespace Store.Catalog.Domain.Commands
{
    public sealed class ChangeProductPriceCommand : ICommand
    {
        public required Guid ProductID { get; set; }
        public required decimal Price { get; set; }
    }
}
