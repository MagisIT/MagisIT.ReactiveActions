using System;

namespace MagisIT.ReactiveActions.Sample.ActionProviders.Descriptors
{
    public class GetProductActionDescriptor : IActionDescriptor
    {
        public string Id { get; }

        public string CombinedIdentifier => Id;

        public GetProductActionDescriptor(string id)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
        }
    }
}
