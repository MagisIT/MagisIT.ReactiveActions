using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MagisIT.ReactiveActions
{
    public class ActionBroker
    {
        private readonly ActionExecutor _actionExecutor;

        internal ActionBroker(ActionExecutor actionExecutor)
        {
            _actionExecutor = actionExecutor ?? throw new ArgumentNullException(nameof(actionExecutor));
        }

        public Task InvokeActionAsync(string trackingSession, string name, IActionDescriptor actionDescriptor = null) =>
            _actionExecutor.InvokeActionAsync(trackingSession, name, actionDescriptor);

        public Task<TResult> InvokeActionAsync<TResult>(string trackingSession, string name, IActionDescriptor actionDescriptor = null) =>
            _actionExecutor.InvokeActionAsync<TResult>(trackingSession, name, actionDescriptor);

        public Task InvokeActionWithoutTrackingAsync(string trackingSession, string name, IActionDescriptor actionDescriptor = null) =>
            _actionExecutor.InvokeActionAsync(trackingSession, name, actionDescriptor,false);

        public Task<TResult> InvokeActionWithoutTrackingAsync<TResult>(string trackingSession, string name, IActionDescriptor actionDescriptor = null) =>
            _actionExecutor.InvokeActionAsync<TResult>(trackingSession, name, actionDescriptor,false);
    }
}
