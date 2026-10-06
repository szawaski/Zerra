using Zerra.CQRS;

namespace Store.Web.Domain.Carts
{
    public sealed class EmptyCartCommand : ICommand
    {
        public required Guid CustomerID { get; set; }
    }
}
