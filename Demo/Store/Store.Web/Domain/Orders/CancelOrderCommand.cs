using Zerra.CQRS;

namespace Store.Web.Domain.Orders
{
    public sealed class CancelOrderCommand : ICommand
    {
        public required Guid OrderID { get; set; }
    }
}
