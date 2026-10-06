using Zerra.CQRS;

namespace Store.Web.Domain.Shipping
{
    public interface IShippingCommandHandler :
        ICommandHandler<MarkDeliveredCommand>
    {
    }
}
