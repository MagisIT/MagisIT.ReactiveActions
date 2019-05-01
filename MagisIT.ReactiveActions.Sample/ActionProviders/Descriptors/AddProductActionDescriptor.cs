using System;

namespace MagisIT.ReactiveActions.Sample.ActionProviders.Descriptors
{
    public class AddProductActionDescriptor : IActionDescriptor
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public double Price { get; set; }

        public int AvailableAmount { get; set; }

        public string CombinedIdentifier => $"{Id}:{Name}:{Price}:{AvailableAmount}";
    }
}
