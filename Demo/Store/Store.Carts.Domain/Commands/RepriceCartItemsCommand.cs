using Zerra.CQRS;

namespace Store.Carts.Domain.Commands
{
    /// <summary>
    /// The Catalog changed a product's price, so every cart holding it takes the new one. Sent by the Catalog service after the price is saved.
    /// </summary>
    /// <remarks>
    /// This is a command and not an event on purpose. An event is fanned out to every replica, which would have them all repricing the same carts,
    /// while a command is handled once no matter how many replicas are running.
    /// </remarks>
    public sealed class RepriceCartItemsCommand : ICommand
    {
        public required Guid ProductID { get; set; }
        public required string ProductName { get; set; }
        public required decimal NewPrice { get; set; }
    }
}
