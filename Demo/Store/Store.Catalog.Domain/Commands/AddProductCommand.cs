using Store.Catalog.Domain.Models;
using Zerra.CQRS;

namespace Store.Catalog.Domain.Commands
{
    public sealed class AddProductCommand : ICommand<AddProductResult>
    {
        public required Guid CategoryID { get; set; }
        public required string Sku { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; }
        public required decimal Price { get; set; }
    }
}
