using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MarcusW.ReactiveActions.Attributes;
using MarcusW.ReactiveActions.Helpers;
using MarcusW.ReactiveActions.Sample.ActionProviders.Descriptors;
using MarcusW.ReactiveActions.Sample.DataSource;
using MarcusW.ReactiveActions.Sample.Models;

namespace MarcusW.ReactiveActions.Sample.ActionProviders
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
