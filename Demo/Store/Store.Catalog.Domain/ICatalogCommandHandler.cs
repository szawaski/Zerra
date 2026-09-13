using Store.Catalog.Domain.Commands;
using Store.Catalog.Domain.Models;
using Zerra.CQRS;

namespace Store.Catalog.Domain
{
    public interface ICatalogCommandHandler :
        ICommandHandler<AddProductCommand, AddProductResult>,
        ICommandHandler<ChangeProductPriceCommand>,
        ICommandHandler<DiscontinueProductCommand>
    {
    }
}
