using System;
using System.Threading.Tasks;
using MagisIT.ReactiveActions.Reactivity.Persistence;
using MagisIT.ReactiveActions.Sample.ActionProviders;
using MagisIT.ReactiveActions.Sample.DataSource;
using MagisIT.ReactiveActions.Sample.Models;
using MagisIT.ReactiveActions.TrackingSessionStore.InMemory;
using MagisIT.ReactiveActions.TrackingSessionStore.Redis;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace MagisIT.ReactiveActions.Sample
{
    public static class Program
    {
        // ReSharper disable once AsyncConverter.AsyncMethodNamingHighlighting
        public static Task Main(string[] args)
        {
            // Build service collection
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IDataSource, SampleDataSource>();
            serviceCollection.AddSingleton(BuildActionBroker);
            serviceCollection.AddSingleton<Sample>();

            IServiceProvider services = serviceCollection.BuildServiceProvider();

            // Run sample
            return services.GetRequiredService<Sample>().RunAsync();
        }

        private static ActionBroker BuildActionBroker(IServiceProvider serviceProvider)
        {
            var redisClient = ConnectionMultiplexer.Connect(new ConfigurationOptions {
                EndPoints = {
                    { "10.200.1.100", 6379 }
                },
                Password = "redis123"
            });

            //ITrackingSessionStore store = new InMemoryStore();
            ITrackingSessionStore store = new RedisStore(redisClient.GetDatabase(), "tracker");

            var builder = new ActionBrokerBuilder(serviceProvider, store);

            builder.AddAction<ProductActions>(nameof(ProductActions.GetProductsAsync));
            builder.AddAction<ProductActions>(nameof(ProductActions.GetProductAsync));
            builder.AddAction<ProductActions>(nameof(ProductActions.AddProductAsync));
            builder.AddAction<ProductActions>(nameof(ProductActions.DeleteProductAsync));
            builder.AddAction<ProductActions>(nameof(ProductActions.GetProductAmountInStockAsync));

            builder.AddAction<ShoppingCartActions>(nameof(ShoppingCartActions.GetCartItemsAsync));
            builder.AddAction<ShoppingCartActions>(nameof(ShoppingCartActions.GetCartItemAsync));
            builder.AddAction<ShoppingCartActions>(nameof(ShoppingCartActions.AddProductToCartAsync));
            builder.AddAction<ShoppingCartActions>(nameof(ShoppingCartActions.RemoveProductFromCartAsync));

            builder.AddModelFilter<Product>(ModelFilters.GetProductsFilter);
            builder.AddModelFilter<Product, string>(ModelFilters.GetProductByIdFilter);
            builder.AddModelFilter<ShoppingCartItem>(ModelFilters.GetCartItemsFilter);
            builder.AddModelFilter<ShoppingCartItem, string>(ModelFilters.GetCartItemForProductIdFilter);

            builder.AddActionResultUpdateHandler(new ConsoleOutputUpdateHandler());

            return builder.Build();
        }
    }
}
