# MagisIT.ReactiveActions
This framework helps you with writing large real-time applications where every data mutation should be automatically propagated to all consumers.
An intelligent filter mechanism detects which data-queries are affected and notifies the data consumers of the changes.
It's intended for use in Web APIs, but it can be used in any other application as well.


![Travis (.org)](https://img.shields.io/travis/MagisIT/MagisIT.ReactiveActions.svg)

## Usage

Any read and write operation against your data source needs to happen through an `Action` that's defined inside of an `ActionProvider` and might be marked as `Reactive` or not.

A simple action provider could look like this:
```C#
public class ProductActions : ActionProviderBase
{
    // IDataSource is injected using the .Net Core dependency injection mechanism
    [Action, ReactiveCollection]
    public Task<ICollection<Product>> GetProductsAsync(IDataSource dataSource)
    {
        // Track the query of all products.
        ICollection<Product> products = TrackCollectionQuery(dataSource.Products, nameof(ModelFilters.GetProductsFilter));
        return Task.FromResult(products);
    }

    [Action, Reactive]
    public Task<Product> GetProductAsync(IDataSource dataSource, GetProductActionDescriptor actionDescriptor)
    {
        // Track the query of a single product and register a filter to recognize this product in later mutations.
        Product product = TrackEntityQuery(dataSource.Products.FirstOrDefault(p => p.Id == actionDescriptor.Id),
                                           nameof(ModelFilters.GetProductByIdFilter),
                                           actionDescriptor.Id);
        return Task.FromResult(product);
    }

    // Actions that have no result must not be marked as [Reactive]
    [Action]
    public Task AddProductAsync(IDataSource dataSource, AddProductActionDescriptor actionDescriptor)
    {
        if (dataSource.Products.Any(p => p.Id == actionDescriptor.Id))
            throw new InvalidOperationException("Product already exists.");

        // Add product to database
        var product = new Product {
            Id = actionDescriptor.Id,
            Name = actionDescriptor.Name,
            Price = actionDescriptor.Price,
            AvailableAmount = actionDescriptor.AvailableAmount
        };
        dataSource.Products.Add(product);
        
        // Track the creation of the new product. This notifies every consumer of the "GetProductsAsync" action.
        return TrackEntityCreatedAsync(product);
    }
}
```

Action parameters are encapsulated in classes that implement `IActionDescriptor`. For example:
```C#
public class AddProductActionDescriptor : IActionDescriptor
{
    // Action parameters
    public string Id { get; set; }
    public string Name { get; set; }
    public double Price { get; set; }
    public int AvailableAmount { get; set; }

    // You need to implement this for every action descriptor. Please make sure the returned string contains all relevant parameters, cleanly delimited by a colon.
    public string CombinedIdentifier => $"{Id}:{Name}:{Price}:{AvailableAmount}";
}
```

Model filters are required to detect which entities are affected by data queries and mutations and can be difined like that:
```C#
public static class ModelFilters
{
    // This matches any product
    public static bool GetProductsFilter(Product product) => true;

    // This matches only the product with the given id. "id" is a static parameter stored alongside with the data query.
    public static bool GetProductByIdFilter(Product product, string id) => product.Id == id;
}
```

Now you can use all this to build an `ActionBroker` that you can use to call the registered actions:
```c#
ITrackingSessionStore store = new InMemoryStore();
var builder = new ActionBrokerBuilder(serviceProvider, store);

builder.AddAction<ProductActions>(nameof(ProductActions.GetProductsAsync));
builder.AddAction<ProductActions>(nameof(ProductActions.GetProductAsync));
builder.AddAction<ProductActions>(nameof(ProductActions.AddProductAsync));

builder.AddModelFilter<Product>(ModelFilters.GetProductsFilter);
builder.AddModelFilter<Product, string>(ModelFilters.GetProductByIdFilter);

// Register any IActionResultUpdateHandler implementation here that handles the detected data updates in your application.
builder.AddActionResultUpdateHandler(new ConsoleOutputUpdateHandler());

IActionBroker actionBroker = builder.Build();
```

Please also take a look into the sample projekt for more information: [Sample](https://github.com/MagisIT/MagisIT.ReactiveActions/tree/master/MagisIT.ReactiveActions.Sample)

## Tracking Session Stores

The data queries of each consumer are tracked inside of a "tracking session" and stored in a data structure that can be queried later to detect who is affected by a data mutation.

You can control where this data structure is stored by passing a `ITrackingSessionStore` implementation.
This library provides two ready-to-use store implementations:
- **InMemoryStore:** Stores everything in concurrent dictionaries. Useful for testing environments.
- **RedisStore:** Uses a `StackExchange.Redis` client to store everything inside of a Redis instance. This is the recommended one, especially in clustered web applications. *Note: This implementation is optimized for Redis and Redis Sentinel. Do not use this with a Redis Cluster.*

## License

This library is licensed under the conditions of the [MIT license](https://github.com/MagisIT/MagisIT.ReactiveActions/blob/master/LICENSE).

Contributions are very appreciated!
