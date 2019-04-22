using System;

namespace MagisIT.ReactiveActions.Sample.ActionProviders.Descriptors
{
    public class GetProductActionDescriptor : IActionDescriptor
    {
        public string Id { get; set; }

        public string CombinedIdentifier => Id;
    }
}
