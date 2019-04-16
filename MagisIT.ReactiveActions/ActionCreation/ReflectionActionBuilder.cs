using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using MagisIT.ReactiveActions.Attributes;

namespace MagisIT.ReactiveActions.ActionCreation
{
    public class ReflectionActionBuilder : IActionBuilder
    {
        public Action BuildAction(IServiceProvider serviceProvider, Type actionProviderType, MethodInfo actionMethod, string actionName)
        {
            if (serviceProvider == null)
                throw new ArgumentNullException(nameof(serviceProvider));
            if (actionProviderType == null)
                throw new ArgumentNullException(nameof(actionProviderType));
            if (actionMethod == null)
                throw new ArgumentNullException(nameof(actionMethod));

            // Search for reactivity attribute
            var reactivityAttribute = actionMethod.GetCustomAttribute<ReactiveAttribute>();

            // Detect action type
            ActionType actionType = ActionType.Default;
            if (reactivityAttribute is ReactiveCollectionAttribute)
                actionType = ActionType.ReactiveCollection;
            else if (reactivityAttribute != null)
                actionType = ActionType.Reactive;

            // Query method parameters
            ParameterInfo[] methodParameters = actionMethod.GetParameters();

            // Build a custom lambda function which abstracts the action execution away
            ActionDelegate actionDelegate = BuildActionDelegate(serviceProvider, actionProviderType, actionMethod, methodParameters);

            return new Action(actionName, actionDelegate, actionType);
        }

        private ActionDelegate BuildActionDelegate(IServiceProvider serviceProvider, Type actionProviderType, MethodInfo actionMethod, ParameterInfo[] methodParameters)
        {
            // TODO: Add further runtime optimizations

            return async (executionContext, actionDescriptor) => {
                if (executionContext == null)
                    throw new ArgumentNullException(nameof(executionContext));

                // Analyse required method parameters
                var paramValues = new List<object>();
                bool actionDescriptionUsed = false;
                foreach (ParameterInfo parameter in methodParameters)
                {
                    // Resolve action description parameter
                    if (typeof(IActionDescriptor).IsAssignableFrom(parameter.ParameterType))
                    {
                        if (actionDescriptor == null)
                            throw new ArgumentNullException(nameof(actionDescriptor), "No action descriptor given.");
                        if (!parameter.ParameterType.IsInstanceOfType(actionDescriptor))
                            throw new ArgumentException("Given action descriptor is of an invalid type.", nameof(actionDescriptor));
                        actionDescriptionUsed = true;

                        paramValues.Add(actionDescriptor);
                        continue;
                    }

                    // Try to resolve the dependency using the service provider
                    var service = serviceProvider.GetService(parameter.ParameterType);
                    if (service != null)
                    {
                        paramValues.Add(service);
                        continue;
                    }

                    // Parameter cannot be resolved, but because it's optional we can pass null.
                    if (parameter.IsOptional)
                    {
                        paramValues.Add(null);
                        continue;
                    }

                    // Resolving dependency failed
                    throw new InvalidOperationException($"Method parameter {parameter.Name} is of an unknown type and cannot be resolved.");
                }

                if (!actionDescriptionUsed && actionDescriptor != null)
                    throw new ArgumentException("This action doesn't expect an action description.", nameof(actionDescriptor));

                // Create action provider instance
                object actionProvider = Activator.CreateInstance(actionProviderType);
                actionProviderType.GetProperty(nameof(IActionProvider.ExecutionContext))?.SetValue(actionProvider, executionContext);

                // Execute method on the new instance
                Task task = (Task)actionMethod.Invoke(actionProvider, paramValues.ToArray());
                await task.ConfigureAwait(false);

                // Return result
                if (actionMethod.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
                    return actionMethod.ReturnType.GetProperty(nameof(Task<object>.Result))?.GetValue(task);
                return null;
            };
        }
    }
}
