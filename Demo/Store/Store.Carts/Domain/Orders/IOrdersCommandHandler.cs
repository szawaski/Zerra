using Zerra.CQRS;

namespace Store.Carts.Domain.Orders
{
    public interface IOrdersCommandHandler :
        ICommandHandler<PlaceOrderCommand, PlaceOrderResult>
    {
    }
}
