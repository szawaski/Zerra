using Zerra.CQRS;

namespace Store.Web.Domain.Shipping
{
    public sealed class MarkDeliveredCommand : ICommand
    {
        public required Guid OrderID { get; set; }
    }
}
