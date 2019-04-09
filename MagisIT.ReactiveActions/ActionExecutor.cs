using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MagisIT.ReactiveActions
{
    public class ActionExecutor
    {
        internal IDictionary<string, ActionDelegate> Actions { get; } = new Dictionary<string, ActionDelegate>();

        internal ActionExecutor() { }

        public Task InvokeActionAsync(string name, IActionDescriptor actionDescriptor = null)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (!Actions.ContainsKey(name))
                throw new ArgumentException($"Action {name} not found.", nameof(name));

            ActionDelegate actionDelegate = Actions[name];
            return actionDelegate.Invoke(actionDescriptor);
        }

        public Task<TResult> InvokeActionAsync<TResult>(string name, IActionDescriptor actionDescriptor = null) => (Task<TResult>)InvokeActionAsync(name, actionDescriptor);
    }
}
