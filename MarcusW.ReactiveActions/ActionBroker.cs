using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MarcusW.ReactiveActions
{
    public delegate Task ActionDelegate(IActionDescriptor actionDescriptor = null);

    public class ActionBroker
    {
        private readonly ActionExecutor _actionExecutor;

        internal ActionBroker(ActionExecutor actionExecutor)
        {
            _actionExecutor = actionExecutor ?? throw new ArgumentNullException(nameof(actionExecutor));
        }

        public Task InvokeActionAsync(string name, IActionDescriptor actionDescriptor = null) => _actionExecutor.InvokeActionAsync(name, actionDescriptor);

        public Task<TResult> InvokeActionAsync<TResult>(string name, IActionDescriptor actionDescriptor = null) =>
            _actionExecutor.InvokeActionAsync<TResult>(name, actionDescriptor);
    }
}
