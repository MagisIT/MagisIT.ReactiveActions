using System;
using MagisIT.ReactiveActions.Sample.Models;

namespace MagisIT.ReactiveActions.Sample
{
    public static class ModelFilters
    {
        public static bool GetProductsFilter(Product product) => true;

        public static bool GetProductByIdFilter(Product product, string id) => product.Id == id;

        public static bool GetCartItemsFilter(ShoppingCartItem cartItem) => true;

        public static bool GetCartItemForProductIdFilter(ShoppingCartItem cartItem, string productId) => cartItem.ProductId == productId;
    }
}
