using Zerra.CQRS;

namespace Store.Web.Domain.Carts
{
    public sealed class AddToCartCommand : ICommand
    {
        public required Guid CustomerID { get; set; }
        public required Guid ProductID { get; set; }
        public required int Quantity { get; set; }
    }
}
