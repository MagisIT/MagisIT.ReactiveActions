using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MagisIT.ReactiveActions.Sample.ActionProviders;
using MagisIT.ReactiveActions.Sample.ActionProviders.Descriptors;
using MagisIT.ReactiveActions.Sample.Models;

namespace MagisIT.ReactiveActions.Sample
{
    public class Sample
    {
        private readonly ActionBroker _actionBroker;

        public Sample(ActionBroker actionBroker)
        {
            _actionBroker = actionBroker ?? throw new ArgumentNullException(nameof(actionBroker));
        }

        public async Task RunAsync()
        {
            string session = "Session1";

            Console.WriteLine($"-> Getting all products as {session}...");
            {
                ICollection<Product> products = await _actionBroker.InvokeAndTrackActionAsync<ICollection<Product>>(session, nameof(ProductActions.GetProductsAsync))
                                                                   .ConfigureAwait(false);
                foreach (Product product in products)
                    PrintProduct(product);
            }

            await PauseAsync().ConfigureAwait(false);

            Console.WriteLine("-> Adding product \"cookies\"...");
            {
                await _actionBroker.InvokeActionAsync(nameof(ProductActions.AddProductAsync),
                                                      new AddProductActionDescriptor {
                                                          Id = "cookies",
                                                          Name = "Cookies",
                                                          Price = 4
                                                      }).ConfigureAwait(false);
            }

            await PauseAsync().ConfigureAwait(false);

            Console.WriteLine($"-> Getting all shopping cart items as {session}...");
            {
                ICollection<ShoppingCartItem> cartItems = await _actionBroker
                                                                .InvokeAndTrackActionAsync<ICollection<ShoppingCartItem>>(session, nameof(ShoppingCartActions.GetCartItemsAsync))
                                                                .ConfigureAwait(false);
                foreach (ShoppingCartItem cartItem in cartItems)
                    PrintShoppingCartItem(cartItem);
            }

            await PauseAsync().ConfigureAwait(false);

            Console.WriteLine("-> Adding product \"milk\" to shopping cart...");
            {
                await _actionBroker.InvokeActionAsync(nameof(ShoppingCartActions.AddProductToCartAsync), new AddProductToCartActionDescriptor { ProductId = "milk", Amount = 3 })
                                   .ConfigureAwait(false);
            }

            await PauseAsync().ConfigureAwait(false);

            Console.WriteLine("-> Adding product \"chocolate\" to shopping cart...");
            {
                await _actionBroker
                      .InvokeActionAsync(nameof(ShoppingCartActions.AddProductToCartAsync), new AddProductToCartActionDescriptor { ProductId = "chocolate", Amount = 2 })
                      .ConfigureAwait(false);
            }

            await PauseAsync().ConfigureAwait(false);

            Console.WriteLine("-> Adding product \"milk\" AGAIN to shopping cart...");
            {
                await _actionBroker.InvokeActionAsync(nameof(ShoppingCartActions.AddProductToCartAsync), new AddProductToCartActionDescriptor { ProductId = "milk", Amount = 5 })
                                   .ConfigureAwait(false);
            }

            await PauseAsync().ConfigureAwait(false);

            Console.WriteLine($"-> Getting all shopping cart items as {session}...");
            {
                ICollection<ShoppingCartItem> cartItems = await _actionBroker
                                                                .InvokeAndTrackActionAsync<ICollection<ShoppingCartItem>>(session, nameof(ShoppingCartActions.GetCartItemsAsync))
                                                                .ConfigureAwait(false);
                foreach (ShoppingCartItem cartItem in cartItems)
                    PrintShoppingCartItem(cartItem);
            }

            await PauseAsync().ConfigureAwait(false);

            Console.WriteLine("-> Deleting product \"milk\"...");
            {
                await _actionBroker.InvokeActionAsync(nameof(ProductActions.DeleteProductAsync), new DeleteProductActionDescriptor { Id = "milk" }).ConfigureAwait(false);
            }

            await PauseAsync().ConfigureAwait(false);

            Console.WriteLine($"-> Getting all products as {session}...");
            {
                ICollection<Product> products = await _actionBroker.InvokeAndTrackActionAsync<ICollection<Product>>(session, nameof(ProductActions.GetProductsAsync))
                                                                   .ConfigureAwait(false);
                foreach (Product product in products)
                    PrintProduct(product);
            }

            Console.WriteLine($"-> Getting all shopping cart items as {session}...");
            {
                ICollection<ShoppingCartItem> cartItems = await _actionBroker
                                                                .InvokeAndTrackActionAsync<ICollection<ShoppingCartItem>>(session, nameof(ShoppingCartActions.GetCartItemsAsync))
                                                                .ConfigureAwait(false);
                foreach (ShoppingCartItem cartItem in cartItems)
                    PrintShoppingCartItem(cartItem);
            }
        }

        private async Task PauseAsync()
        {
            await Task.Delay(1000).ConfigureAwait(false);
            Console.WriteLine();
        }

        private void PrintProduct(Product product) => Console.WriteLine($"    Product: Id: {product.Id} | Name: {product.Name} | Price: {product.Price}");

        private void PrintShoppingCartItem(ShoppingCartItem cartItem) => Console.WriteLine($"    ShoppingCartItem: ProductId: {cartItem.ProductId} | Amount: {cartItem.Amount}");
    }
}
