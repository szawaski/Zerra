using Zerra.CQRS;

namespace Store.Web.Domain.Carts
{
    public sealed class RemoveFromCartCommand : ICommand
    {
        public required Guid CustomerID { get; set; }
        public required Guid ProductID { get; set; }
    }
}
