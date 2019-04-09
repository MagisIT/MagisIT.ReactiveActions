using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace MagisIT.ReactiveActions.ActionCreation
{
    public class ReflectionActionBuilder : IActionBuilder
    {
        public ActionDelegate BuildActionDelegate(IServiceProvider serviceProvider, ActionExecutor actionExecutor, Type actionProviderType, MethodInfo actionMethod)
        {
            if (serviceProvider == null)
                throw new ArgumentNullException(nameof(serviceProvider));
            if (actionExecutor == null)
                throw new ArgumentNullException(nameof(actionExecutor));
            if (actionProviderType == null)
                throw new ArgumentNullException(nameof(actionProviderType));
            if (actionMethod == null)
                throw new ArgumentNullException(nameof(actionMethod));

            // Query method parameters
            ParameterInfo[] methodParameters = actionMethod.GetParameters();

            // Return a custom lambda function which abstracts the action execution away
            return actionDescriptor => {
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
                actionProviderType.GetProperty(nameof(IActionProvider.ActionExecutor))?.SetValue(actionProvider, actionExecutor);

                // Execute method on the new instance
                return (Task)actionMethod.Invoke(actionProvider, paramValues.ToArray());
            };
        }
    }
}
