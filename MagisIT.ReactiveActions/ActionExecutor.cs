using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MagisIT.ReactiveActions.Reactivity;
using MagisIT.ReactiveActions.Reactivity.Persistence;
using MagisIT.ReactiveActions.Reactivity.Persistence.Models;
using MagisIT.ReactiveActions.Reactivity.UpdateHandling;

namespace MagisIT.ReactiveActions
{
    public class ActionExecutor
    {
        private readonly IReadOnlyDictionary<string, Action> _actions;

        private readonly IReadOnlyDictionary<string, ModelFilter> _modelFilters;

        private readonly IReadOnlyCollection<IActionResultUpdateHandler> _actionResultUpdateHandlers;

        private readonly ITrackingSessionStore _trackingSessionStore;

        internal ActionExecutor(IReadOnlyDictionary<string, Action> actions,
                                IReadOnlyDictionary<string, ModelFilter> modelFilters,
                                IReadOnlyCollection<IActionResultUpdateHandler> actionResultUpdateHandlers,
                                ITrackingSessionStore trackingSessionStore)
        {
            _actions = actions ?? throw new ArgumentNullException(nameof(actions));
            _modelFilters = modelFilters ?? throw new ArgumentNullException(nameof(modelFilters));
            _actionResultUpdateHandlers = actionResultUpdateHandlers ?? throw new ArgumentNullException(nameof(actionResultUpdateHandlers));
            _trackingSessionStore = trackingSessionStore ?? throw new ArgumentNullException(nameof(trackingSessionStore));
        }

        public Task<object> InvokeActionAsync(string trackingSession, string name, IActionDescriptor actionDescriptor = null, bool trackingEnabled = true) =>
            InternalInvokeRootActionAsync(trackingSession, name, actionDescriptor, trackingEnabled);

        public async Task<TResult> InvokeActionAsync<TResult>(string trackingSession, string name, IActionDescriptor actionDescriptor = null, bool trackingEnabled = true)
        {
            return (TResult)await InternalInvokeRootActionAsync(trackingSession, name, actionDescriptor, trackingEnabled).ConfigureAwait(false);
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

        public async Task PublishModelUpdateAsync<TModel>(string trackingSession, TModel beforeUpdate, TModel afterUpdate) where TModel : class
        {
            if (trackingSession == null)
                throw new ArgumentNullException(nameof(trackingSession));
            if (beforeUpdate == null && afterUpdate == null)
                throw new ArgumentNullException(nameof(afterUpdate));

            // Get all previous data queries for entities of this model type
            IEnumerable<DataQuery> dataQueriesForModel = await _trackingSessionStore.GetDataQueriesForModelAsync(trackingSession, typeof(TModel).Name).ConfigureAwait(false);

            // TODO: check for created/updated/deleted and call update handler
        }

        private async Task<object> InternalInvokeRootActionAsync(string trackingSession, string name, IActionDescriptor actionDescriptor = null, bool trackingEnabled = true)
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
            ExecutionContext executionContext = ExecutionContext.CreateRootContext(trackingSession, trackingEnabled, this, action);

            object result = await action.ExecuteAsync(executionContext, actionDescriptor).ConfigureAwait(false);

            // Store tracked data queries of action call
            if (trackingEnabled)
                await StoreExecutionAsync(executionContext, actionDescriptor).ConfigureAwait(false);

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

        private Task StoreExecutionAsync(ExecutionContext rootContext, IActionDescriptor actionDescriptor)
        {
            string trackingSession = rootContext.TrackingSession;
            string actionName = rootContext.Action.Name;
            string actionDescriptorTypeName = actionDescriptor.GetType().Name;

            var actionCall = new ActionCall {
                TrackingSession = trackingSession,
                Id = $"{actionName}:{actionDescriptorTypeName}:{actionDescriptor.CombinedIdentifier}",
                ActionName = actionName,
                ActionDescriptorTypeName = actionDescriptorTypeName,
                ActionDescriptor = actionDescriptor
            };
            var dataQueries = new List<DataQuery>();

            // Extract the data queries recursively from the execution contexts
            void VisitExecutionContext(ExecutionContext context)
            {
                dataQueries.AddRange(context.DataQueries.Select(filter => new DataQuery {
                    TrackingSession = trackingSession,
                    Id = filter.Identifier,
                    ModelTypeName = filter.ModelFilter.ModelType.Name,
                    FilterName = filter.ModelFilter.Name,
                    FilterParams = filter.FilterParams,
                    AffectedActionCalls = new[] {
                        new ActionCallReference {
                            ActionCallId = actionCall.Id,
                            Direct = context == rootContext
                        }
                    }
                }));

                foreach (ExecutionContext subContext in context.SubContexts)
                    VisitExecutionContext(subContext);
            }

            VisitExecutionContext(rootContext);

            // Merge data queries with similar ID
            // If at least two data queries have the same ID. Favour the one that is direct.
            DataQuery[] mergedDataQueries = dataQueries.GroupBy(dataQuery => dataQuery.Id)
                                                       .Select(grouping => grouping.OrderBy(entry => entry.AffectedActionCalls.First().Direct ? 0 : 1).FirstOrDefault()).ToArray();

            return _trackingSessionStore.StoreTrackedActionCallAsync(trackingSession, actionCall, mergedDataQueries);
        }
    }
}
