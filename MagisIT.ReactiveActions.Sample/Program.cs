using System;
using System.Threading.Tasks;
using MagisIT.ReactiveActions.Sample.ActionProviders;
using MagisIT.ReactiveActions.Sample.DataSource;
using Microsoft.Extensions.DependencyInjection;

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
            var builder = new ActionBrokerBuilder(serviceProvider);

            builder.AddAction<ProductActions>(nameof(ProductActions.GetProductsAsync));
            builder.AddAction<ProductActions>(nameof(ProductActions.GetProductAsync));

            return builder.Build();
        }
    }
}
