using Zerra.CQRS;

namespace Store.Web.Domain.Carts
{
    public sealed class CheckoutCartCommand : ICommand<CheckoutCartResult>
    {
        public required Guid CustomerID { get; set; }
    }
}
