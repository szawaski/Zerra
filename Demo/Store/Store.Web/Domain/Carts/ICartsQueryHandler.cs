using Zerra.CQRS;

namespace Store.Web.Domain.Carts
{
    public interface ICartsQueryHandler : IQueryHandler
    {
        Task<string> GetDataStoreName(CancellationToken cancellationToken);
        Task<string> GetMessagingName(CancellationToken cancellationToken);

        /// <summary>
        /// The customer's cart, rebuilt by replaying its events. A customer who never added anything has an empty cart.
        /// </summary>
        Task<CartModel> GetCart(Guid customerID, CancellationToken cancellationToken);

        /// <summary>
        /// The most recent events in the customer's cart, each with the cart as it was right after that event.
        /// </summary>
        Task<CartHistoryModel[]> GetCartHistory(Guid customerID, CancellationToken cancellationToken);
    }
}
