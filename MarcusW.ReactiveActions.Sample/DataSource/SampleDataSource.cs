using System.Collections.Generic;
using MarcusW.ReactiveActions.Sample.Models;

namespace MarcusW.ReactiveActions.Sample.DataSource
{
    public class SampleDataSource : IDataSource
    {
        public List<Product> Products { get; } = new List<Product> {
            new Product {
                Id = "milk", Name = "1L Milk", Price = 1
            },
            new Product {
                Id = "chocolate", Name = "1 Bar of Chocolate", Price = 1.5
            }
        };
    }
}
