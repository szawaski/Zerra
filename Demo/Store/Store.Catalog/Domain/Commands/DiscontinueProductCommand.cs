using Zerra.CQRS;

namespace Store.Catalog.Domain.Commands
{
    public sealed class DiscontinueProductCommand : ICommand
    {
        public required Guid ProductID { get; set; }
    }
}
