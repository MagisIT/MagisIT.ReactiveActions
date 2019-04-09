using System;
using System.Threading.Tasks;
using MarcusW.ReactiveActions.Sample.ActionProviders;
using MarcusW.ReactiveActions.Sample.ActionProviders.Descriptors;
using MarcusW.ReactiveActions.Sample.Models;

namespace MarcusW.ReactiveActions.Sample
{
    public class Sample
    {
        private readonly ActionBroker _actionBroker;

        public Sample(ActionBroker actionBroker)
        {
            _actionBroker = actionBroker ?? throw new ArgumentNullException(nameof(actionBroker));
        }

        public async Task RunAsync()
        {
            var product = await _actionBroker.InvokeActionAsync<Product>(nameof(ProductActions.GetProductAsync), new GetProductActionDescriptor("milk")).ConfigureAwait(false);
        }
    }
}
