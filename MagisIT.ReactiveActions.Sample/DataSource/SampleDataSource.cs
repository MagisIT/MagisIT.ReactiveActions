using System.Collections.Concurrent;
using System.Collections.Generic;
using MagisIT.ReactiveActions.Sample.Models;

namespace MagisIT.ReactiveActions.Sample.DataSource
{
    /// <summary>
    /// A dummy implementation of a data source for demonstration purposes.
    /// This class is not thread-safe for simplicity reasons.
    /// </summary>
    public class SampleDataSource : IDataSource
    {
        public IList<Product> Products { get; } = new List<Product> {
            new Product {
                Id = "milk", Name = "1L Milk", Price = 1
            },
            new Product {
                Id = "chocolate", Name = "1 Bar of Chocolate", Price = 1.5
            }
        };

        public IList<ShoppingCartItem> ShoppingCartItems { get; } = new List<ShoppingCartItem>();
    }
}
