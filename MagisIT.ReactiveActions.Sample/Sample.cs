using System;
using System.Threading.Tasks;
using MagisIT.ReactiveActions.Sample.ActionProviders;
using MagisIT.ReactiveActions.Sample.ActionProviders.Descriptors;
using MagisIT.ReactiveActions.Sample.Models;

namespace MagisIT.ReactiveActions.Sample
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
            string session = "Session1";
            var product = await _actionBroker.InvokeActionAsync<Product>(session, nameof(ProductActions.GetProductAsync), new GetProductActionDescriptor("milk"))
                                             .ConfigureAwait(false);
        }
    }
}
