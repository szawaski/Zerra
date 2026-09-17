using Store.Carts.Domain;
using Store.Carts.Domain.Commands;
using Store.Carts.Domain.Events;
using Store.Carts.Domain.Models;
using Store.Carts.Service.Aggregates;
using Store.Catalog.Domain;
using Store.Common;
using Store.Orders.Domain.Commands;
using Store.Orders.Domain.Models;
using Zerra.CQRS;
using Zerra.Repository;

namespace Store.Carts.Service.Handlers
{
    /// <summary>
    /// Each command rebuilds the customer's cart from its events, checks the rules against that state, and appends one new event.
    /// Appending with <c>validateEventNumber</c> fails if another event reached the stream after the rebuild, so two commands on the same cart can't both act on stale state.
    /// </summary>
    public sealed class CartsCommandHandler : BaseHandler, ICartsCommandHandler
    {
        //the same limits the Orders service puts on an order, so a cart can always be checked out
        private const int maxItems = 20;
        private const int maxQuantity = 100;

        public async Task Handle(AddToCartCommand command, CancellationToken cancellationToken)
        {
            if (command.Quantity < 1 || command.Quantity > maxQuantity)
                throw new DomainException($"Quantity must be between 1 and {maxQuantity}.");

            var cart = await RebuildCart(command.CustomerID);

            var item = cart.Items.FirstOrDefault(x => x.ProductID == command.ProductID);
            if (item is null && cart.Items.Count >= maxItems)
                throw new DomainException($"A cart can have at most {maxItems} different products.");
            if (item is not null && item.Quantity + command.Quantity > maxQuantity)
                throw new DomainException($"A cart can have at most {maxQuantity} of {item.ProductName}.");

            //the name and price come from the Catalog service, never from the browser
            var products = await Bus.Call<ICatalogQueryHandler>().GetProductsByIDs([command.ProductID], cancellationToken);
            var product = products.FirstOrDefault() ?? throw new DomainException("Product not found.");
            if (!product.IsActive)
                throw new DomainException($"{product.Name} has been discontinued.");

            await cart.Append(new CartItemAddedEvent()
            {
                CustomerID = command.CustomerID,
                ProductID = product.ID,
                ProductName = product.Name!,
                UnitPrice = product.Price,
                Quantity = command.Quantity
            }, true);

            Log?.Info($"Added {command.Quantity} x {product.Name} to cart {command.CustomerID}");
        }

        public async Task Handle(RemoveFromCartCommand command, CancellationToken cancellationToken)
        {
            var cart = await RebuildCart(command.CustomerID);

            var item = cart.Items.FirstOrDefault(x => x.ProductID == command.ProductID) ?? throw new DomainException("That product isn't in the cart.");

            await cart.Append(new CartItemRemovedEvent() { CustomerID = command.CustomerID, ProductID = command.ProductID }, true);

            Log?.Info($"Removed {item.ProductName} from cart {command.CustomerID}");
        }

        public async Task Handle(EmptyCartCommand command, CancellationToken cancellationToken)
        {
            var cart = await RebuildCart(command.CustomerID);

            if (cart.Items.Count == 0)
                throw new DomainException("The cart is already empty.");

            await cart.Append(new CartEmptiedEvent() { CustomerID = command.CustomerID }, true);

            Log?.Info($"Emptied cart {command.CustomerID}");
        }

        public async Task<CheckoutCartResult> Handle(CheckoutCartCommand command, CancellationToken cancellationToken)
        {
            var cart = await RebuildCart(command.CustomerID);

            if (cart.Items.Count == 0)
                throw new DomainException("The cart is empty.");

            //Command the Orders service and wait: it reprices from the Catalog and reserves stock, and if that fails the cart is left as it is
            var order = await Bus.DispatchAwaitAsync(new PlaceOrderCommand()
            {
                CustomerID = command.CustomerID,
                Items = cart.Items.Select(x => new OrderItemRequest() { ProductID = x.ProductID, Quantity = x.Quantity }).ToArray()
            });

            await cart.Append(new CartCheckedOutEvent()
            {
                CustomerID = command.CustomerID,
                OrderID = order.OrderID,
                OrderNumber = order.OrderNumber!,
                Total = order.Total
            }, true);

            Log?.Info($"Checked out cart {command.CustomerID} as order {order.OrderNumber}");
            return new CheckoutCartResult() { OrderID = order.OrderID, OrderNumber = order.OrderNumber, Total = order.Total };
        }

        private async Task<CartAggregate> RebuildCart(Guid customerID)
        {
            var cart = new CartAggregate(customerID, Context.GetService<IEventStoreEngine>());
            //false when the customer has never had a cart, the first append creates the stream
            _ = await cart.Rebuild();
            return cart;
        }
    }
}
