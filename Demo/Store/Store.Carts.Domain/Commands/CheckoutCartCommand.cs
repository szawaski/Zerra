using Store.Carts.Domain.Models;
using Zerra.CQRS;

namespace Store.Carts.Domain.Commands
{
    public sealed class CheckoutCartCommand : ICommand<CheckoutCartResult>
    {
        public required Guid CustomerID { get; set; }
    }
}
