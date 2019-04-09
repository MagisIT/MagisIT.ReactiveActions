using System;

namespace MarcusW.ReactiveActions.Sample.ActionProviders.Descriptors
{
    public class GetProductActionDescriptor : IActionDescriptor
    {
        public string Id { get; }

        public GetProductActionDescriptor(string id)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
        }
    }
}
