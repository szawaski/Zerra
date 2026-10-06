using Store.Carts.Domain.Commands;
using Store.Carts.Domain.Models;
using Zerra.CQRS;

namespace Store.Carts.Domain
{
    public interface ICartsCommandHandler :
        ICommandHandler<AddToCartCommand>,
        ICommandHandler<RemoveFromCartCommand>,
        ICommandHandler<EmptyCartCommand>,
        ICommandHandler<CheckoutCartCommand, CheckoutCartResult>
    {
    }
}
