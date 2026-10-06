using Store.Shipping.Domain.Commands;
using Zerra.CQRS;

namespace Store.Shipping.Domain
{
    public interface IShippingCommandHandler :
        ICommandHandler<MarkDeliveredCommand>
    {
    }
}
