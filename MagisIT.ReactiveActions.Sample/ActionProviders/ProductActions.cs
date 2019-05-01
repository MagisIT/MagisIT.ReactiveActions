using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MagisIT.ReactiveActions.Attributes;
using MagisIT.ReactiveActions.Helpers;
using MagisIT.ReactiveActions.Sample.ActionProviders.Descriptors;
using MagisIT.ReactiveActions.Sample.DataSource;
using MagisIT.ReactiveActions.Sample.Models;

namespace MagisIT.ReactiveActions.Sample.ActionProviders
{
    public class ProductActions : ActionProviderBase
    {
        [Action, ReactiveCollection]
        public Task<ICollection<Product>> GetProductsAsync(IDataSource dataSource)
        {
            ICollection<Product> products = TrackCollectionQuery(dataSource.Products, nameof(ModelFilters.GetProductsFilter));
            return Task.FromResult(products);
        }

        [Action, Reactive]
        public Task<Product> GetProductAsync(IDataSource dataSource, GetProductActionDescriptor actionDescriptor)
        {
            Product product = TrackEntityQuery(dataSource.Products.FirstOrDefault(p => p.Id == actionDescriptor.Id),
                                               nameof(ModelFilters.GetProductByIdFilter),
                                               actionDescriptor.Id);
            return Task.FromResult(product);
        }

        [Action]
        public Task AddProductAsync(IDataSource dataSource, AddProductActionDescriptor actionDescriptor)
        {
            if (dataSource.Products.Any(p => p.Id == actionDescriptor.Id))
                throw new InvalidOperationException("Product already exists.");

            var product = new Product {
                Id = actionDescriptor.Id,
                Name = actionDescriptor.Name,
                Price = actionDescriptor.Price,
                AvailableAmount = actionDescriptor.AvailableAmount
            };

            dataSource.Products.Add(product);
            return TrackEntityCreatedAsync(product);
        }

        [Action]
        public async Task DeleteProductAsync(IDataSource dataSource, DeleteProductActionDescriptor actionDescriptor)
        {
            Product product = dataSource.Products.FirstOrDefault(p => p.Id == actionDescriptor.Id);
            if (product == null)
                return;

            // Ensure this product isn't referenced from cart items
            await InvokeActionAsync(nameof(ShoppingCartActions.RemoveProductFromCartAsync), new RemoveProductFromCartActionDescriptor { ProductId = product.Id })
                .ConfigureAwait(false);

            dataSource.Products.Remove(product);
            await TrackEntityDeletedAsync(product).ConfigureAwait(false);
        }

        [Action, Reactive]
        public async Task<int> GetProductAmountInStockAsync(GetProductAmountInStockActionDescriptor actionDescriptor)
        {
            // When any result below changes, this action will change, too.
            var product = await InvokeActionAsync<Product>(nameof(GetProductAsync), new GetProductActionDescriptor { Id = actionDescriptor.ProductId }).ConfigureAwait(false);
            var cartItem = await InvokeActionAsync<ShoppingCartItem>(nameof(ShoppingCartActions.GetCartItemAsync),
                                                                     new GetCartItemActionDescriptor { ProductId = actionDescriptor.ProductId }).ConfigureAwait(false);

            int amount = product.AvailableAmount;
            if (cartItem != null)
                amount -= cartItem.Amount;
            return amount;
        }
    }
}
