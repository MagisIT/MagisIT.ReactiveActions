using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using MagisIT.ReactiveActions.ActionCreation;
using MagisIT.ReactiveActions.Attributes;

namespace MagisIT.ReactiveActions
{
    public class ActionBrokerBuilder
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IActionBuilder _actionBuilder;

        internal ActionExecutor ActionExecutor { get; } = new ActionExecutor();

        public ActionBrokerBuilder(IServiceProvider serviceProvider, IActionBuilder actionBuilder)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _actionBuilder = actionBuilder ?? throw new ArgumentNullException(nameof(actionBuilder));
        }

        public ActionBrokerBuilder(IServiceProvider serviceProvider) : this(serviceProvider, new ReflectionActionBuilder()) { }

        public void AddAction<TActionProvider>(string actionMethodName) where TActionProvider : IActionProvider, new()
        {
            if (actionMethodName == null)
                throw new ArgumentNullException(nameof(actionMethodName));
            if (ActionExecutor.Actions.ContainsKey(actionMethodName))
                throw new ArgumentException($"An action method with the same name is already registered: {actionMethodName}", nameof(actionMethodName));

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

            // Construct action delegate
            ActionDelegate actionDelegate = _actionBuilder.BuildActionDelegate(_serviceProvider, ActionExecutor, actionProviderType, actionMethod);

            // Register action
            ActionExecutor.Actions.Add(actionMethodName, actionDelegate);
        }

        public ActionBroker Build()
        {
            return new ActionBroker(ActionExecutor);
        }
    }
}
