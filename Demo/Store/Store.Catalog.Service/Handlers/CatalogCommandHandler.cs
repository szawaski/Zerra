using Store.Carts.Domain;
using Store.Carts.Domain.Commands;
using Store.Catalog.Domain;
using Store.Catalog.Domain.Commands;
using Store.Catalog.Domain.Events;
using Store.Catalog.Domain.Models;
using Store.Catalog.Service.Data;
using Store.Common;
using Zerra;
using Zerra.Repository;

namespace Store.Catalog.Service.Handlers
{
    public sealed class CatalogCommandHandler : BaseHandlerWithRepo, ICatalogCommandHandler
    {
        private const decimal maxPrice = 100_000m;

        public async Task<AddProductResult> Handle(AddProductCommand command, CancellationToken cancellationToken)
        {
            var sku = command.Sku?.Trim().ToUpperInvariant() ?? String.Empty;
            if (sku.Length == 0 || sku.Length > 32)
                throw new DomainException("SKU is required and must be 32 characters or less.");
            if (!sku.All(c => Char.IsAsciiLetterOrDigit(c) || c == '-'))
                throw new DomainException("SKU may only contain letters, digits, and dashes.");

            var name = command.Name?.Trim() ?? String.Empty;
            if (name.Length == 0 || name.Length > 128)
                throw new DomainException("Name is required and must be 128 characters or less.");

            var description = String.IsNullOrWhiteSpace(command.Description) ? null : command.Description.Trim();
            if (description is not null && description.Length > 512)
                throw new DomainException("Description must be 512 characters or less.");

            ValidatePrice(command.Price);

            if (!await Repo.AnyAsync<CategoryDataModel>(x => x.ID == command.CategoryID))
                throw new DomainException("Category not found.");
            if (await Repo.AnyAsync<ProductDataModel>(x => x.Sku == sku))
                throw new DomainException($"SKU {sku} is already in use.");

            var product = new ProductDataModel()
            {
                ID = Guid.NewGuid(),
                CategoryID = command.CategoryID,
                Sku = sku,
                Name = name,
                Description = description,
                Price = command.Price,
                Status = nameof(ProductStatus.Active)
            };
            await Repo.CreateAsync(product);

            Log?.Info($"Added product {product.Sku} {product.Name} at {product.Price:0.00}");
            return new AddProductResult() { ProductID = product.ID };
        }

        public async Task Handle(ChangeProductPriceCommand command, CancellationToken cancellationToken)
        {
            var product = await Repo.SingleAsync<ProductDataModel>(x => x.ID == command.ProductID) ?? throw new DomainException("Product not found.");
            if (product.Status == nameof(ProductStatus.Discontinued))
                throw new DomainException($"{product.Name} is discontinued, its price can't be changed.");
            ValidatePrice(command.Price);

            var oldPrice = product.Price;
            if (oldPrice == command.Price)
                throw new DomainException($"{product.Name} is already {command.Price:0.00}.");

            product.Price = command.Price;
            //the graph limits the update to the price column
            await Repo.UpdateAsync(product, new Graph<ProductDataModel>(x => x.Price));

            //The same price change needs both kinds of message, which is the clearest place in this demo to see the difference.
            //An event is fanned out: every subscribing replica gets a copy, so every one of them drops its cached copy of the product.
            await Bus.DispatchAsync(new ProductPriceChangedEvent()
            {
                ProductID = product.ID,
                Name = product.Name!,
                OldPrice = oldPrice,
                NewPrice = product.Price
            });
            //A command is handled once: repricing the carts must happen one time, not once per replica.
            await Bus.DispatchAsync(new RepriceCartItemsCommand()
            {
                ProductID = product.ID,
                ProductName = product.Name!,
                NewPrice = product.Price
            });

            Log?.Info($"Changed price of {product.Sku} from {oldPrice:0.00} to {product.Price:0.00}");
        }

        public async Task Handle(DiscontinueProductCommand command, CancellationToken cancellationToken)
        {
            var product = await Repo.SingleAsync<ProductDataModel>(x => x.ID == command.ProductID) ?? throw new DomainException("Product not found.");
            if (product.Status == nameof(ProductStatus.Discontinued))
                throw new DomainException($"{product.Name} is already discontinued.");

            product.Status = nameof(ProductStatus.Discontinued);
            await Repo.UpdateAsync(product, new Graph<ProductDataModel>(x => x.Status));

            //same reason as the price change: every subscriber replica has to drop its own cached copy
            await Bus.DispatchAsync(new ProductDiscontinuedEvent() { ProductID = product.ID, Name = product.Name! });

            Log?.Info($"Discontinued {product.Sku} {product.Name}");
        }

        private static void ValidatePrice(decimal price)
        {
            if (price <= 0 || price > maxPrice)
                throw new DomainException($"Price must be greater than 0 and no more than {maxPrice:N0}.");
            if (Decimal.Round(price, 2) != price)
                throw new DomainException("Price can't have more than two decimal places.");
        }
    }
}
