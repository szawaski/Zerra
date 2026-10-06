using Store.Orders.Domain.Commands;
using Store.Orders.Domain.Models;
using Zerra.CQRS;

namespace Store.Orders.Domain
{
    public interface IOrdersCommandHandler :
        ICommandHandler<PlaceOrderCommand, PlaceOrderResult>,
        ICommandHandler<CancelOrderCommand>,
        ICommandHandler<ShipOrderCommand>
    {
    }
}
