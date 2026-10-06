using Zerra.CQRS;

namespace Store.Web.Domain.Catalog
{
    public sealed class ChangeProductPriceCommand : ICommand
    {
        public required Guid ProductID { get; set; }
        public required decimal Price { get; set; }
    }
}
