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
        public Task<List<Product>> GetProductsAsync(IDataSource dataSource)
        {
            return Task.FromResult(dataSource.Products);
        }

        [Action]
        public Task<Product> GetProductAsync(IDataSource dataSource, GetProductActionDescriptor actionDescriptor)
        {
            return Task.FromResult(dataSource.Products.FirstOrDefault(p => p.Id == actionDescriptor.Id));
        }
    }
}
