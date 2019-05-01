using System;

namespace MagisIT.ReactiveActions.Sample.ActionProviders.Descriptors
{
    public class GetProductAmountInStockActionDescriptor : IActionDescriptor
    {
        public string ProductId { get; set; }

        public string CombinedIdentifier => ProductId;
    }
}
