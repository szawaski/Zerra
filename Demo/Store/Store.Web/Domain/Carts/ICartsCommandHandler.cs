using Zerra.CQRS;

namespace Store.Web.Domain.Carts
{
    public interface ICartsCommandHandler :
        ICommandHandler<AddToCartCommand>,
        ICommandHandler<RemoveFromCartCommand>,
        ICommandHandler<EmptyCartCommand>,
        ICommandHandler<CheckoutCartCommand, CheckoutCartResult>
    {
    }
}
