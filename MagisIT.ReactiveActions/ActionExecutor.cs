using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MagisIT.ReactiveActions.Reactivity;
using MagisIT.ReactiveActions.Reactivity.UpdateHandling;

namespace MagisIT.ReactiveActions
{
    public class ActionExecutor
    {
        private readonly IReadOnlyDictionary<string, ActionDelegate> _actions;

        private readonly IReadOnlyDictionary<string, ModelFilter> _modelFilters;

        private readonly IReadOnlyCollection<IActionResultUpdateHandler> _actionResultUpdateHandlers;

        internal ActionExecutor(IReadOnlyDictionary<string, ActionDelegate> actions,
                                IReadOnlyDictionary<string, ModelFilter> modelFilters,
                                IReadOnlyCollection<IActionResultUpdateHandler> actionResultUpdateHandlers)
        {
            _actions = actions ?? throw new ArgumentNullException(nameof(actions));
            _modelFilters = modelFilters ?? throw new ArgumentNullException(nameof(modelFilters));
            _actionResultUpdateHandlers = actionResultUpdateHandlers ?? throw new ArgumentNullException(nameof(actionResultUpdateHandlers));
        }

        public Task InvokeActionAsync(string name, IActionDescriptor actionDescriptor = null) => InternalInvokeActionAsync(name, actionDescriptor);

        public Task<TResult> InvokeActionAsync<TResult>(string name, IActionDescriptor actionDescriptor = null) => (Task<TResult>)InternalInvokeActionAsync(name, actionDescriptor);

        public Task InvokeSubActionAsync(ExecutionContext currentExecutionContext, string name, IActionDescriptor actionDescriptor = null)
        {
            if (currentExecutionContext == null)
                throw new ArgumentNullException(nameof(currentExecutionContext));

            return InternalInvokeActionAsync(name, actionDescriptor, currentExecutionContext);
        }

        public Task<TResult> InvokeSubActionAsync<TResult>(ExecutionContext currentExecutionContext, string name, IActionDescriptor actionDescriptor = null)
        {
            if (currentExecutionContext == null)
                throw new ArgumentNullException(nameof(currentExecutionContext));

            return (Task<TResult>)InternalInvokeActionAsync(name, actionDescriptor, currentExecutionContext);
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

        private Task InternalInvokeActionAsync(string name, IActionDescriptor actionDescriptor = null, ExecutionContext currentExecutionContext = null)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (!_actions.ContainsKey(name))
                throw new ArgumentException($"Action {name} not found.", nameof(name));

            // The context for the following execution
            ExecutionContext nextExecutionContext;

            // Is this the root level of the action execution tree?
            if (currentExecutionContext == null)
                nextExecutionContext = ExecutionContext.CreateRootContext(this);
            else
                nextExecutionContext = currentExecutionContext.CreateSubContext();

            ActionDelegate actionDelegate = _actions[name];
            return actionDelegate.Invoke(nextExecutionContext, actionDescriptor);
        }
    }
}
