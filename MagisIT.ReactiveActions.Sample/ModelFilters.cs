using System;
using MagisIT.ReactiveActions.Sample.Models;

namespace MagisIT.ReactiveActions.Sample
{
    public static class ModelFilters
    {
        public static bool GetProductsFilter(Product product) => true;

        public static bool GetProductByIdFilter(Product product, string id) => product.Id == id;
    }
}
