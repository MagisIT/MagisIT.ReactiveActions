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

namespace MagisIT.ReactiveActions
{
    public class ActionBrokerBuilder
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IActionBuilder _actionBuilder;
        private readonly ITrackingSessionStore _trackingSessionStore;

        private readonly IDictionary<string, Action> _actions = new Dictionary<string, Action>();
        private readonly IDictionary<string, ModelFilter> _modelFilters = new Dictionary<string, ModelFilter>();
        private readonly IList<IActionResultUpdateHandler> _actionResultUpdateHandlers = new List<IActionResultUpdateHandler>();

        private bool _built = false;

        public ActionBrokerBuilder(IServiceProvider serviceProvider, IActionBuilder actionBuilder, ITrackingSessionStore trackingSessionStore)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _actionBuilder = actionBuilder ?? throw new ArgumentNullException(nameof(actionBuilder));
            _trackingSessionStore = trackingSessionStore ?? throw new ArgumentNullException(nameof(trackingSessionStore));
        }

        public ActionBrokerBuilder(IServiceProvider serviceProvider, ITrackingSessionStore trackingSessionStore) : this(
            serviceProvider,
            new ReflectionActionBuilder(),
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
                throw new ArgumentException("Action method not found in action provider class.", nameof(actionMethodName));
            if (!typeof(Task).IsAssignableFrom(actionMethod.ReturnType))
                throw new ArgumentException("Action method does not return a task.", nameof(actionMethodName));
            Debug.Assert(actionMethodName == actionMethod.Name);

            // Get action attribute
            var actionAttribute = actionMethod.GetCustomAttribute<ActionAttribute>();
            if (actionAttribute == null)
                throw new ArgumentException("Action method has no Action attribute", nameof(actionMethodName));

            // Construct action
            Action action = _actionBuilder.BuildAction(_serviceProvider, actionProviderType, actionMethod, actionMethodName);

            // Register action
            _actions.Add(actionMethodName, action);

            return this;
        }

        public ActionBrokerBuilder AddModelFilter<TModel>(Func<TModel, bool> filterDelegate)
        {
            InternalAddModelFilter<TModel>(filterDelegate);
            return this;
        }

        public ActionBrokerBuilder AddModelFilter<TModel, T1>(Func<TModel, T1, bool> filterDelegate)
        {
            InternalAddModelFilter<TModel>(filterDelegate);
            return this;
        }

        public ActionBrokerBuilder AddModelFilter<TModel, T1, T2>(Func<TModel, T1, T2, bool> filterDelegate)
        {
            InternalAddModelFilter<TModel>(filterDelegate);
            return this;
        }

        public ActionBrokerBuilder AddModelFilter<TModel, T1, T2, T3>(Func<TModel, T1, T2, T3, bool> filterDelegate)
        {
            InternalAddModelFilter<TModel>(filterDelegate);
            return this;
        }

        public ActionBrokerBuilder AddModelFilter<TModel, T1, T2, T3, T4>(Func<TModel, T1, T2, T3, T4, bool> filterDelegate)
        {
            InternalAddModelFilter<TModel>(filterDelegate);
            return this;
        }

        public ActionBrokerBuilder AddModelFilter<TModel, T1, T2, T3, T4, T5>(Func<TModel, T1, T2, T3, T4, T5, bool> filterDelegate)
        {
            InternalAddModelFilter<TModel>(filterDelegate);
            return this;
        }

        public ActionBrokerBuilder AddModelFilter<TModel, T1, T2, T3, T4, T5, T6>(Func<TModel, T1, T2, T3, T4, T5, T6, bool> filterDelegate)
        {
            InternalAddModelFilter<TModel>(filterDelegate);
            return this;
        }

        public ActionBrokerBuilder AddModelFilter<TModel, T1, T2, T3, T4, T5, T6, T7>(Func<TModel, T1, T2, T3, T4, T5, T6, T7, bool> filterDelegate)
        {
            InternalAddModelFilter<TModel>(filterDelegate);
            return this;
        }

        public ActionBrokerBuilder AddModelFilter<TModel, T1, T2, T3, T4, T5, T6, T7, T8>(Func<TModel, T1, T2, T3, T4, T5, T6, T7, T8, bool> filterDelegate)
        {
            InternalAddModelFilter<TModel>(filterDelegate);
            return this;
        }

        public ActionBrokerBuilder AddModelFilter<TModel, T1, T2, T3, T4, T5, T6, T7, T8, T9>(Func<TModel, T1, T2, T3, T4, T5, T6, T7, T8, T9, bool> filterDelegate)
        {
            InternalAddModelFilter<TModel>(filterDelegate);
            return this;
        }

        public ActionBrokerBuilder AddModelFilter<TModel, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(Func<TModel, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, bool> filterDelegate)
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

        private void InternalAddModelFilter<TModel>(Delegate filterDelegate)
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
