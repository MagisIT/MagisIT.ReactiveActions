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
    public class ShoppingCartActions : ActionProviderBase
    {
        [Action, ReactiveCollection]
        public Task<ICollection<ShoppingCartItem>> GetCartItemsAsync(IDataSource dataSource)
        {
            ICollection<ShoppingCartItem> cartItems = TrackCollectionQuery(true, dataSource.ShoppingCartItems, nameof(ModelFilters.GetCartItemsFilter));
            return Task.FromResult(cartItems);
        }

        [Action, Reactive]
        public Task<ShoppingCartItem> GetCartItemAsync(IDataSource dataSource, GetCartItemActionDescriptor actionDescriptor)
        {
            ShoppingCartItem cartItem = TrackEntityQuery(true,
                                                         dataSource.ShoppingCartItems.FirstOrDefault(i => i.ProductId == actionDescriptor.ProductId),
                                                         nameof(ModelFilters.GetCartItemForProductIdFilter),
                                                         actionDescriptor.ProductId);
            return Task.FromResult(cartItem);
        }

        [Action]
        public async Task AddProductToCartAsync(IDataSource dataSource, AddProductToCartActionDescriptor actionDescriptor)
        {
            // Ensure the product exists
            var product = await InvokeActionAsync<Product>(nameof(ProductActions.GetProductAsync), new GetProductActionDescriptor { Id = actionDescriptor.ProductId })
                .ConfigureAwait(false);
            if (product == null)
                throw new ArgumentException($"Product {actionDescriptor.ProductId} doesn't exists.", nameof(actionDescriptor));

            // If the product already is in the cart, increase the amount.
            ShoppingCartItem item = dataSource.ShoppingCartItems.FirstOrDefault(i => i.ProductId == product.Id);
            if (item != null)
            {
                var previousItem = item.ShallowCopy();
                item.Amount += actionDescriptor.Amount;
                await TrackEntityChangedAsync(previousItem, item).ConfigureAwait(false);
            }
            else
            {
                item = new ShoppingCartItem { ProductId = product.Id, Amount = actionDescriptor.Amount };
                dataSource.ShoppingCartItems.Add(item);
                await TrackEntityCreatedAsync(item).ConfigureAwait(false);
            }
        }

        [Action]
        public async Task RemoveProductFromCartAsync(IDataSource dataSource, RemoveProductFromCartActionDescriptor actionDescriptor)
        {
            ShoppingCartItem item = dataSource.ShoppingCartItems.FirstOrDefault(i => i.ProductId == actionDescriptor.ProductId);
            if (item == null)
                return;

            dataSource.ShoppingCartItems.Remove(item);
            await TrackEntityDeletedAsync(item).ConfigureAwait(false);
        }
    }
}
