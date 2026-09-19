using Store.Carts.Domain;
using Store.Carts.Domain.Commands;
using Store.Carts.Domain.Models;
using Store.Carts.Service.Aggregates;
using Store.Carts.Service.Data;
using Store.Catalog.Domain;
using Store.Common;
using Store.Orders.Domain;
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
    public sealed class CartsCommandHandler : BaseHandler, ICartsCommandHandler, ICartRepricingHandler
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

            //the name and price come from the Catalog service, never from the browser, and are cached until the Catalog says otherwise
            var cache = Context.GetService<ICatalogProductCache>();
            if (!cache.TryGet(command.ProductID, out var product))
            {
                var products = await Bus.Call<ICatalogQueryHandler>().GetProductsByIDs([command.ProductID], cancellationToken);
                product = products.FirstOrDefault();
                if (product is not null)
                    cache.Set(product);
            }
            if (product is null)
                throw new DomainException("Product not found.");
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

        /// <summary>
        /// Sent by the Catalog service when a price changes, so no cart shows a price the store no longer charges.
        /// A command and not an event: an event is fanned out to every replica, a command is handled by one of them.
        /// </summary>
        public async Task Handle(RepriceCartItemsCommand command, CancellationToken cancellationToken)
        {
            //An event store answers "what happened to this cart", not "which carts hold this product", so every customer's cart is replayed.
            //A store with more than a handful of customers would keep a projection of carts by product and only touch those.
            var customers = await Bus.Call<IOrdersQueryHandler>().GetCustomers(cancellationToken);
            var eventStore = Context.GetService<IEventStoreEngine>();

            var carts = 0;
            foreach (var customer in customers)
            {
                var cart = new CartAggregate(customer.ID, eventStore);
                //false when the customer has never had a cart, there is no stream to reprice
                if (!await cart.Rebuild())
                    continue;

                var item = cart.Items.FirstOrDefault(x => x.ProductID == command.ProductID);
                //a command can be delivered twice, a cart already at the new price has nothing to append
                if (item is null || item.UnitPrice == command.NewPrice)
                    continue;

                await cart.Append(new CartItemRepricedEvent()
                {
                    CustomerID = customer.ID,
                    ProductID = command.ProductID,
                    ProductName = item.ProductName,
                    OldUnitPrice = item.UnitPrice,
                    NewUnitPrice = command.NewPrice
                }, true);
                carts++;
            }

            Log?.Info($"Repriced {command.ProductName} to {command.NewPrice:0.00} in {carts} cart(s)");
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
