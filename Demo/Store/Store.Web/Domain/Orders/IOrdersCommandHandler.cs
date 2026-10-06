using Zerra.CQRS;

namespace Store.Web.Domain.Orders
{
    public interface IOrdersCommandHandler :
        ICommandHandler<PlaceOrderCommand, PlaceOrderResult>,
        ICommandHandler<CancelOrderCommand>,
        ICommandHandler<ShipOrderCommand>
    {
    }
}
