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
                Price = actionDescriptor.Price
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
    }
}
