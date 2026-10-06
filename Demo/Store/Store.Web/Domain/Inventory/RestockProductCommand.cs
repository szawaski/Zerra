using Zerra.CQRS;

namespace Store.Web.Domain.Inventory
{
    public sealed class RestockProductCommand : ICommand
    {
        public required Guid ProductID { get; set; }
        public required int Quantity { get; set; }
    }
}
