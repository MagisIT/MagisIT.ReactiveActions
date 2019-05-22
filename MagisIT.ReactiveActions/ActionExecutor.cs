using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using MagisIT.ReactiveActions.Reactivity;
using MagisIT.ReactiveActions.Reactivity.Persistence;
using MagisIT.ReactiveActions.Reactivity.Persistence.Models;
using MagisIT.ReactiveActions.Reactivity.UpdateHandling;

namespace MagisIT.ReactiveActions
{
    public class ActionExecutor : IActionExecutor
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

        public Task<object> InvokeActionAsync(string name, IActionDescriptor actionDescriptor = null, IActionArguments actionArguments = null, string trackingSession = null) =>
            InternalInvokeRootActionAsync(name, actionDescriptor, actionArguments, trackingSession);

        public async Task<TResult> InvokeActionAsync<TResult>(string name,
                                                              IActionDescriptor actionDescriptor = null,
                                                              IActionArguments actionArguments = null,
                                                              string trackingSession = null)
        {
            return (TResult)await InternalInvokeRootActionAsync(name, actionDescriptor, actionArguments, trackingSession).ConfigureAwait(false);
        }

        public Task<object> InvokeSubActionAsync(IExecutionContext currentExecutionContext,
                                                 string name,
                                                 IActionDescriptor actionDescriptor = null,
                                                 IActionArguments actionArguments = null)
        {
            if (currentExecutionContext == null)
                throw new ArgumentNullException(nameof(currentExecutionContext));

            return InternalInvokeSubActionAsync(currentExecutionContext, name, actionDescriptor, actionArguments);
        }

        public async Task<TResult> InvokeSubActionAsync<TResult>(IExecutionContext currentExecutionContext,
                                                                 string name,
                                                                 IActionDescriptor actionDescriptor = null,
                                                                 IActionArguments actionArguments = null)
        {
            if (currentExecutionContext == null)
                throw new ArgumentNullException(nameof(currentExecutionContext));

            return (TResult)await InternalInvokeSubActionAsync(currentExecutionContext, name, actionDescriptor, actionArguments).ConfigureAwait(false);
        }

        public ModelFilter GetModelFilter(string name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (!_modelFilters.ContainsKey(name))
                throw new ArgumentException($"Cannot find model filter for name: {name}", nameof(name));

            return _modelFilters[name];
        }

        public ModelFilter GetModelFilter<TModel>(string name) where TModel : class
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            // Check if the model filter can be used for the specified model type
            ModelFilter modelFilter = GetModelFilter(name);
            if (!modelFilter.CanFilterModelType(typeof(TModel)))
                throw new ArgumentException("Model type is incompatible to selected model filter.", nameof(TModel));

            return modelFilter;
        }

        public async Task PublishModelUpdateAsync<TModel>(TModel updatedModel, TModel oldModel = null) where TModel : class
        {
            if (updatedModel == null && oldModel == null)
                throw new ArgumentNullException(nameof(updatedModel));

            // Get all previous data queries for entities of this model type
            IEnumerable<DataQuery> dataQueriesForModel = await _trackingSessionStore.GetGlobalDataQueriesForModelAsync(typeof(TModel).Name).ConfigureAwait(false);

            // Filter for queries that are affected by this model update
            IEnumerable<DataQuery> affectedQueries = dataQueriesForModel.Where(query => {
                if (!_modelFilters.ContainsKey(query.FilterName))
                    return false;
                ModelFilter modelFilter = _modelFilters[query.FilterName];

                // It's faster to just catch exceptions instead of validating the input using reflection.
                try
                {
                    // Test if the filter matches for the updated/old model.
                    return updatedModel != null && modelFilter.Matches(updatedModel, query.FilterParams) || oldModel != null && modelFilter.Matches(oldModel, query.FilterParams);
                }
                catch (Exception ex) when (ex is MemberAccessException || ex is TargetInvocationException)
                {
                    // The filter parameters were wrong. Ignore this query.
                    return false;
                }
            });

            // Handle affected tracking session seperated from each other
            // TODO: Think about performance optimizations because of the many parallel running Tasks. Maybe Parallel.Foreach()?
            await Task.WhenAll(affectedQueries.GroupBy(query => query.TrackingSession).Select(sessionGroup => {
                string trackingSession = sessionGroup.Key;
                IEnumerable<DataQuery> sessionQueries = sessionGroup;

                // Merge List of affected action calls
                // If action calls are affected more than once, favour the one that is indirect because indirect updates have a greater affect than direct updates.
                IEnumerable<ActionCallReference> affectedActionCalls = sessionQueries.SelectMany(query => query.AffectedActionCalls).GroupBy(call => call.ActionCallId)
                                                                                     .Select(grouping => grouping.OrderBy(entry => entry.Direct).First());

                // Notify update handlers of affected action calls
                return Task.WhenAll(affectedActionCalls.Select(async reference => {
                    // Get action call
                    ActionCall actionCall = await _trackingSessionStore.GetActionCallAsync(trackingSession, reference.ActionCallId).ConfigureAwait(false);
                    if (!_actions.ContainsKey(actionCall.ActionName))
                        return;

                    // Search for the registered action
                    Action action = _actions[actionCall.ActionName];
                    if (!action.IsReactive)
                        return;

                    // Call update handlers
                    if (action.IsReactiveCollection && reference.Direct && action.ResultModelType == typeof(TModel))
                    {
                        await Task.WhenAll(_actionResultUpdateHandlers.Select(
                                               handler => handler.HandleResultItemChangedAsync(trackingSession, action, actionCall.ActionDescriptor, oldModel, updatedModel)))
                                  .ConfigureAwait(false);
                    }
                    else
                    {
                        await Task.WhenAll(_actionResultUpdateHandlers.Select(handler => handler.HandleResultChangedAsync(trackingSession, action, actionCall.ActionDescriptor)))
                                  .ConfigureAwait(false);
                    }
                }));
            })).ConfigureAwait(false);
        }

        private async Task<object> InternalInvokeRootActionAsync(string name,
                                                                 IActionDescriptor actionDescriptor = null,
                                                                 IActionArguments actionArguments = null,
                                                                 string trackingSession = null)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (!_actions.ContainsKey(name))
                throw new ArgumentException($"Action {name} not found.", nameof(name));

            bool trackingEnabled = trackingSession != null;

            Action action = _actions[name];
            if (trackingEnabled && !action.IsReactive)
                throw new ArgumentException("Non-reactive actions cannot be tracked", nameof(trackingEnabled));

            // Create root level of the execution tree
            IExecutionContext executionContext = ExecutionContext.CreateRootContext(this, action, trackingSession);

            object result = await action.ExecuteAsync(executionContext, actionDescriptor, actionArguments).ConfigureAwait(false);

            // Store tracked data queries of action call
            if (trackingEnabled)
                await StoreExecutionAsync(executionContext, actionDescriptor).ConfigureAwait(false);

            return result;
        }

        private Task<object> InternalInvokeSubActionAsync(IExecutionContext currentExecutionContext,
                                                          string name,
                                                          IActionDescriptor actionDescriptor = null,
                                                          IActionArguments actionArguments = null)
        {
            if (currentExecutionContext == null)
                throw new ArgumentNullException(nameof(currentExecutionContext));
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (!_actions.ContainsKey(name))
                throw new ArgumentException($"Action {name} not found.", nameof(name));

            // The context for the following execution
            Action action = _actions[name];
            IExecutionContext nextExecutionContext = currentExecutionContext.CreateSubContext(action);

            return action.ExecuteAsync(nextExecutionContext, actionDescriptor, actionArguments);
        }

        private Task StoreExecutionAsync(IExecutionContext rootContext, IActionDescriptor actionDescriptor = null)
        {
            string trackingSession = rootContext.TrackingSession;
            string actionName = rootContext.Action.Name;
            string actionDescriptorTypeName = actionDescriptor?.GetType().Name;

            string actionCallId = $"{actionName}:";
            if (actionDescriptor == null)
                actionCallId += "%";
            else
                actionCallId += $"{actionDescriptorTypeName}:{actionDescriptor.CombinedIdentifier}";

            var actionCall = new ActionCall {
                TrackingSession = trackingSession,
                Id = actionCallId,
                ActionName = actionName,
                ActionDescriptorTypeName = actionDescriptorTypeName,
                ActionDescriptor = actionDescriptor
            };
            var dataQueries = new List<DataQuery>();

            // Extract the data queries recursively from the execution contexts
            void VisitExecutionContext(IExecutionContext context)
            {
                dataQueries.AddRange(context.DataQueries.Select(filter => new DataQuery {
                    TrackingSession = trackingSession,
                    Id = filter.Identifier,
                    ModelTypeName = filter.ModelFilter.ModelType.Name,
                    FilterName = filter.ModelFilter.Name,
                    FilterParams = filter.FilterParams,
                    AffectedActionCalls = new[] { new ActionCallReference { ActionCallId = actionCall.Id, Direct = context == rootContext } }
                }));

                foreach (IExecutionContext subContext in context.SubContexts)
                    VisitExecutionContext(subContext);
            }

            VisitExecutionContext(rootContext);

            // Merge data queries with similar ID
            // If at least two data queries have the same ID. Favour the one that is indirect because indirect updates have a greater affect than direct updates.
            DataQuery[] mergedDataQueries = dataQueries.GroupBy(dataQuery => dataQuery.Id)
                                                       .Select(grouping => grouping.OrderBy(entry => entry.AffectedActionCalls.First().Direct).First()).ToArray();

            return _trackingSessionStore.StoreTrackedActionCallAsync(trackingSession, actionCall, mergedDataQueries);
        }
    }
}
