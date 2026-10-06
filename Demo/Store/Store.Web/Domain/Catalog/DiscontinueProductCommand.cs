using Zerra.CQRS;

namespace Store.Web.Domain.Catalog
{
    public sealed class DiscontinueProductCommand : ICommand
    {
        public required Guid ProductID { get; set; }
    }
}
