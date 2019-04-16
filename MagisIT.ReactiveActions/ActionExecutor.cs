using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MagisIT.ReactiveActions.Reactivity;
using MagisIT.ReactiveActions.Reactivity.UpdateHandling;

namespace MagisIT.ReactiveActions
{
    public class ActionExecutor
    {
        private readonly IReadOnlyDictionary<string, Action> _actions;

        private readonly IReadOnlyDictionary<string, ModelFilter> _modelFilters;

        private readonly IReadOnlyCollection<IActionResultUpdateHandler> _actionResultUpdateHandlers;

        internal ActionExecutor(IReadOnlyDictionary<string, Action> actions,
                                IReadOnlyDictionary<string, ModelFilter> modelFilters,
                                IReadOnlyCollection<IActionResultUpdateHandler> actionResultUpdateHandlers)
        {
            _actions = actions ?? throw new ArgumentNullException(nameof(actions));
            _modelFilters = modelFilters ?? throw new ArgumentNullException(nameof(modelFilters));
            _actionResultUpdateHandlers = actionResultUpdateHandlers ?? throw new ArgumentNullException(nameof(actionResultUpdateHandlers));
        }

        public Task<object> InvokeActionAsync(string trackingSession, string name, IActionDescriptor actionDescriptor = null, bool registerTracker = true) =>
            InternalInvokeRootActionAsync(trackingSession, name, actionDescriptor, registerTracker);

        public async Task<TResult> InvokeActionAsync<TResult>(string trackingSession, string name, IActionDescriptor actionDescriptor = null, bool registerTracker = true)
        {
            return (TResult)await InternalInvokeRootActionAsync(trackingSession, name, actionDescriptor).ConfigureAwait(false);
        }

        public Task<object> InvokeSubActionAsync(ExecutionContext currentExecutionContext, string name, IActionDescriptor actionDescriptor = null)
        {
            if (currentExecutionContext == null)
                throw new ArgumentNullException(nameof(currentExecutionContext));

            return InternalInvokeSubActionAsync(currentExecutionContext, name, actionDescriptor);
        }

        public async Task<TResult> InvokeSubActionAsync<TResult>(ExecutionContext currentExecutionContext, string name, IActionDescriptor actionDescriptor = null)
        {
            if (currentExecutionContext == null)
                throw new ArgumentNullException(nameof(currentExecutionContext));

            return (TResult)await InternalInvokeSubActionAsync(currentExecutionContext, name, actionDescriptor).ConfigureAwait(false);
        }

        public ModelFilter GetModelFilter(string name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (!_modelFilters.ContainsKey(name))
                throw new ArgumentException($"Cannot find model filter for name: {name}", nameof(name));

            return _modelFilters[name];
        }

        public ModelFilter GetModelFilter<TModel>(string name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            // Check if the model filter can be used for the specified model type
            ModelFilter modelFilter = GetModelFilter(name);
            if (!modelFilter.CanFilterModelType(typeof(TModel)))
                throw new ArgumentException("Model type is incompatible to selected model filter.", nameof(TModel));

            return modelFilter;
        }

        private async Task<object> InternalInvokeRootActionAsync(string trackingSession, string name, IActionDescriptor actionDescriptor = null, bool registerTracker = true)
        {
            if (trackingSession == null)
                throw new ArgumentNullException(nameof(trackingSession));
            if (string.IsNullOrWhiteSpace(trackingSession))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(trackingSession));
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (!_actions.ContainsKey(name))
                throw new ArgumentException($"Action {name} not found.", nameof(name));

            // Create root level of the execution tree
            Action action = _actions[name];
            ExecutionContext executionContext = ExecutionContext.CreateRootContext(trackingSession, registerTracker, this, action);


            object result = await action.ExecuteAsync(executionContext, actionDescriptor).ConfigureAwait(false);

            // Register tracker
            if (registerTracker) { }

            return result;
        }

        private Task<object> InternalInvokeSubActionAsync(ExecutionContext currentExecutionContext, string name, IActionDescriptor actionDescriptor = null)
        {
            if (currentExecutionContext == null)
                throw new ArgumentNullException(nameof(currentExecutionContext));
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (!_actions.ContainsKey(name))
                throw new ArgumentException($"Action {name} not found.", nameof(name));

            // The context for the following execution
            Action action = _actions[name];
            ExecutionContext nextExecutionContext = currentExecutionContext.CreateSubContext(action);

            return action.ExecuteAsync(nextExecutionContext, actionDescriptor);
        }
    }
}
