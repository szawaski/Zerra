using Store.Catalog.Domain.Carts;
using Zerra.CQRS;

namespace Store.Catalog.Test
{
    public sealed class FakeCartRepricingHandler : BaseHandler, ICartRepricingHandler
    {
        public List<RepriceCartItemsCommand> Commands { get; } = new();

        public Task Handle(RepriceCartItemsCommand command, CancellationToken cancellationToken)
        {
            Commands.Add(command);
            return Task.CompletedTask;
        }
    }
}
