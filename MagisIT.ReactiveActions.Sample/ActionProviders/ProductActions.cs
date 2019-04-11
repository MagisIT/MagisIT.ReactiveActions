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
        [Action]
        [ReactiveCollection]
        public Task<ICollection<Product>> GetProductsAsync(IDataSource dataSource)
        {
            var products = TrackCollectionQuery(dataSource.Products, nameof(ModelFilters.GetProductsFilter));
            return Task.FromResult(products);
        }

        [Action]
        [Reactive]
        public async Task<Product> GetProductAsync(IDataSource dataSource, GetProductActionDescriptor actionDescriptor)
        {
            var test = await InvokeActionAsync<ICollection<Product>>(nameof(GetProductsAsync)).ConfigureAwait(false);

            return TrackEntityQuery(dataSource.Products.FirstOrDefault(p => p.Id == actionDescriptor.Id), nameof(ModelFilters.GetProductByIdFilter), actionDescriptor.Id);
        }
    }
}
