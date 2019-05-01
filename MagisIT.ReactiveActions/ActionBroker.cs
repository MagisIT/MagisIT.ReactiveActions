using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MagisIT.ReactiveActions
{
    public class ActionBroker : IActionBroker
    {
        private readonly IActionExecutor _actionExecutor;

        internal ActionBroker(IActionExecutor actionExecutor)
        {
            _actionExecutor = actionExecutor ?? throw new ArgumentNullException(nameof(actionExecutor));
        }

        public Task<object> InvokeAndTrackActionAsync(string trackingSession, string name, IActionDescriptor actionDescriptor = null) =>
            _actionExecutor.InvokeActionAsync(name, actionDescriptor, trackingSession);

        public Task<TResult> InvokeAndTrackActionAsync<TResult>(string trackingSession, string name, IActionDescriptor actionDescriptor = null) =>
            _actionExecutor.InvokeActionAsync<TResult>(name, actionDescriptor, trackingSession);

        public Task<object> InvokeActionAsync(string name, IActionDescriptor actionDescriptor = null) =>
            _actionExecutor.InvokeActionAsync(name, actionDescriptor);

        public Task<TResult> InvokeActionAsync<TResult>(string name, IActionDescriptor actionDescriptor = null) =>
            _actionExecutor.InvokeActionAsync<TResult>(name, actionDescriptor);
    }
}
