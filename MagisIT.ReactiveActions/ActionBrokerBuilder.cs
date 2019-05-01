using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using MagisIT.ReactiveActions.ActionCreation;
using MagisIT.ReactiveActions.Attributes;
using MagisIT.ReactiveActions.Reactivity;
using MagisIT.ReactiveActions.Reactivity.Persistence;
using MagisIT.ReactiveActions.Reactivity.UpdateHandling;
using MarcusW.SharpUtils.Core.Extensions;

namespace MagisIT.ReactiveActions
{
    public class ActionBrokerBuilder
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IActionDelegateBuilder _actionDelegateBuilder;
        private readonly ITrackingSessionStore _trackingSessionStore;

        private readonly IDictionary<string, Action> _actions = new Dictionary<string, Action>();
        private readonly IDictionary<string, ModelFilter> _modelFilters = new Dictionary<string, ModelFilter>();
        private readonly IList<IActionResultUpdateHandler> _actionResultUpdateHandlers = new List<IActionResultUpdateHandler>();

        private bool _built = false;

        public ActionBrokerBuilder(IServiceProvider serviceProvider, IActionDelegateBuilder actionDelegateBuilder, ITrackingSessionStore trackingSessionStore)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _actionDelegateBuilder = actionDelegateBuilder ?? throw new ArgumentNullException(nameof(actionDelegateBuilder));
            _trackingSessionStore = trackingSessionStore ?? throw new ArgumentNullException(nameof(trackingSessionStore));
        }

        public ActionBrokerBuilder(IServiceProvider serviceProvider, ITrackingSessionStore trackingSessionStore) : this(
            serviceProvider,
            new ReflectionActionDelegateBuilder(),
            trackingSessionStore) { }

        public ActionBrokerBuilder AddAction<TActionProvider>(string actionMethodName) where TActionProvider : IActionProvider, new()
        {
            if (actionMethodName == null)
                throw new ArgumentNullException(nameof(actionMethodName));
            if (_actions.ContainsKey(actionMethodName))
                throw new ArgumentException($"An action method with the same name is already registered: {actionMethodName}", nameof(actionMethodName));
            AssertNotBuilt();

            Type actionProviderType = typeof(TActionProvider);

            // Search for action method
            MethodInfo actionMethod = actionProviderType.GetMethod(actionMethodName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod);
            if (actionMethod == null || actionMethod.DeclaringType != actionProviderType)
                throw new ArgumentException($"Action method {actionMethodName} not found in action provider class {actionProviderType.Name}.", nameof(actionMethodName));
            Debug.Assert(actionMethodName == actionMethod.Name);

            // Get action attribute
            var actionAttribute = actionMethod.GetCustomAttribute<ActionAttribute>();
            if (actionAttribute == null)
                throw new ArgumentException($"Action method {actionMethodName} has no Action attribute", nameof(actionMethodName));

            // Search for reactivity attribute
            var reactivityAttribute = actionMethod.GetCustomAttribute<ReactiveAttribute>();

            // Detect action type
            ActionType actionType = ActionType.Default;
            if (reactivityAttribute is ReactiveCollectionAttribute)
                actionType = ActionType.ReactiveCollection;
            else if (reactivityAttribute != null)
                actionType = ActionType.Reactive;

            // Check return type of the action method
            Type returnType = actionMethod.ReturnType;
            if (!typeof(Task).IsAssignableFrom(returnType))
                throw new ArgumentException($"Action method {actionMethodName} does not return a task.", nameof(actionMethodName));

            // Get result type based on action type
            Type resultType, resultModelType;
            if (actionType.HasFlag(ActionType.Reactive))
            {
                if (returnType.GetGenericTypeDefinition() != typeof(Task<>))
                    throw new ArgumentException($"Reactive actions need to return a result: {actionMethodName}", nameof(actionMethodName));
                resultType = returnType.GenericTypeArguments[0];

                // Is the result a collection of models?
                if (actionType.HasFlag(ActionType.ReactiveCollection))
                {
                    // Result should be a collection of entities
                    if (!typeof(ICollection<>).IsGenericTypeDefinitionAssignableFrom(resultType))
                        throw new ArgumentException($"Reactive collection actions need to return a collection of entities: {actionMethodName}", nameof(actionMethodName));
                    resultModelType = resultType.GenericTypeArguments[0];
                }
                else
                {
                    // The result should be one single entity (will be checked later)
                    resultModelType = resultType;
                }
            }
            else
            {
                // We know it's a task. Now check, that's it not an Task<> or something
                if (returnType != typeof(Task))
                    throw new ArgumentException($"Non-reactive action methods must not return a result: {actionMethodName}", nameof(actionMethodName));
                resultModelType = resultType = null;
            }

            // The result model should be a simple class, not a collection or something like that
            if (resultModelType != null && ((!resultModelType.IsClass && !resultModelType.IsPrimitive) || resultModelType.IsGenericType))
                throw new ArgumentException($"Reactive actions should return simple non-generic objects or collections of them: {actionMethodName}", nameof(actionMethod));

            // Construct action delegate
            ActionDelegate actionDelegate = _actionDelegateBuilder.BuildActionDelegate(_serviceProvider, actionProviderType, actionMethod);
            if (actionDelegate == null)
                throw new InvalidOperationException("Built action delegate must not be null.");

            // Create a new action
            var action = new Action(actionMethodName, actionDelegate, actionMethod, actionType, resultType, resultModelType);

            // Register action
            _actions.Add(actionMethodName, action);

            return this;
        }

        public ActionBrokerBuilder AddModelFilter<TModel>(Func<TModel, bool> filterDelegate) where TModel : class
        {
            InternalAddModelFilter<TModel>(filterDelegate);
            return this;
        }

        public ActionBrokerBuilder AddModelFilter<TModel, T1>(Func<TModel, T1, bool> filterDelegate) where TModel : class
        {
            InternalAddModelFilter<TModel>(filterDelegate);
            return this;
        }

        public ActionBrokerBuilder AddModelFilter<TModel, T1, T2>(Func<TModel, T1, T2, bool> filterDelegate) where TModel : class
        {
            InternalAddModelFilter<TModel>(filterDelegate);
            return this;
        }

        public ActionBrokerBuilder AddModelFilter<TModel, T1, T2, T3>(Func<TModel, T1, T2, T3, bool> filterDelegate) where TModel : class
        {
            InternalAddModelFilter<TModel>(filterDelegate);
            return this;
        }

        public ActionBrokerBuilder AddModelFilter<TModel, T1, T2, T3, T4>(Func<TModel, T1, T2, T3, T4, bool> filterDelegate) where TModel : class
        {
            InternalAddModelFilter<TModel>(filterDelegate);
            return this;
        }

        public ActionBrokerBuilder AddModelFilter<TModel, T1, T2, T3, T4, T5>(Func<TModel, T1, T2, T3, T4, T5, bool> filterDelegate) where TModel : class
        {
            InternalAddModelFilter<TModel>(filterDelegate);
            return this;
        }

        public ActionBrokerBuilder AddModelFilter<TModel, T1, T2, T3, T4, T5, T6>(Func<TModel, T1, T2, T3, T4, T5, T6, bool> filterDelegate) where TModel : class
        {
            InternalAddModelFilter<TModel>(filterDelegate);
            return this;
        }

        public ActionBrokerBuilder AddModelFilter<TModel, T1, T2, T3, T4, T5, T6, T7>(Func<TModel, T1, T2, T3, T4, T5, T6, T7, bool> filterDelegate) where TModel : class
        {
            InternalAddModelFilter<TModel>(filterDelegate);
            return this;
        }

        public ActionBrokerBuilder AddModelFilter<TModel, T1, T2, T3, T4, T5, T6, T7, T8>(Func<TModel, T1, T2, T3, T4, T5, T6, T7, T8, bool> filterDelegate) where TModel : class
        {
            InternalAddModelFilter<TModel>(filterDelegate);
            return this;
        }

        public ActionBrokerBuilder AddModelFilter<TModel, T1, T2, T3, T4, T5, T6, T7, T8, T9>(Func<TModel, T1, T2, T3, T4, T5, T6, T7, T8, T9, bool> filterDelegate)
            where TModel : class
        {
            InternalAddModelFilter<TModel>(filterDelegate);
            return this;
        }

        public ActionBrokerBuilder AddModelFilter<TModel, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(Func<TModel, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, bool> filterDelegate)
            where TModel : class
        {
            InternalAddModelFilter<TModel>(filterDelegate);
            return this;
        }

        public ActionBrokerBuilder AddActionResultUpdateHandler(IActionResultUpdateHandler updateHandler)
        {
            AssertNotBuilt();

            if (updateHandler == null)
                throw new ArgumentNullException(nameof(updateHandler));

            // Register handler
            _actionResultUpdateHandlers.Add(updateHandler);

            return this;
        }

        public ActionBroker Build()
        {
            AssertNotBuilt();

            var actionExecutor = new ActionExecutor((IReadOnlyDictionary<string, Action>)_actions,
                                                    (IReadOnlyDictionary<string, ModelFilter>)_modelFilters,
                                                    (IReadOnlyCollection<IActionResultUpdateHandler>)_actionResultUpdateHandlers,
                                                    _trackingSessionStore);
            var actionBroker = new ActionBroker(actionExecutor);

            _built = true;
            return actionBroker;
        }

        private void InternalAddModelFilter<TModel>(Delegate filterDelegate) where TModel : class
        {
            if (filterDelegate == null)
                throw new ArgumentNullException(nameof(filterDelegate));
            AssertNotBuilt();

            // Check if the method is anoymous (e.g. a lambda) and therefore the naming will be unexpected
            MethodInfo methodInfo = filterDelegate.Method;
            if (methodInfo.GetCustomAttribute<CompilerGeneratedAttribute>(false) != null)
                throw new ArgumentException("The filter delegate must not be an anonymous method.", nameof(filterDelegate));

            // Ensure all parameters are simply serializable
            if (methodInfo.GetParameters().Skip(1).Any(p => !p.ParameterType.IsPrimitive && p.ParameterType != typeof(string)))
                throw new ArgumentException("The filter parameters must be primitive types or strings.", nameof(filterDelegate));

            // Check if this filter is already registered
            string filterName = methodInfo.Name;
            if (_actions.ContainsKey(filterName))
                throw new ArgumentException($"A model filter with the same name is already registered: {filterName}", nameof(filterDelegate));

            // Register filter
            var filter = new ModelFilter(typeof(TModel), filterName, filterDelegate);
            _modelFilters.Add(filterName, filter);
        }

        private void AssertNotBuilt()
        {
            if (_built)
                throw new InvalidOperationException("Builder methods are unavailable after the ActionBroker has been built.");
        }
    }
}
