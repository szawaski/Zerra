using Zerra.CQRS;

namespace Store.Web.Domain.Catalog
{
    public interface ICatalogCommandHandler :
        ICommandHandler<AddProductCommand, AddProductResult>,
        ICommandHandler<ChangeProductPriceCommand>,
        ICommandHandler<DiscontinueProductCommand>
    {
    }
}
