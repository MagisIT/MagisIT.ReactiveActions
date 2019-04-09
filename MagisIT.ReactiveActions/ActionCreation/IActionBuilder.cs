using System;
using System.Reflection;

namespace MagisIT.ReactiveActions.ActionCreation
{
    public interface IActionBuilder
    {
        ActionDelegate BuildActionDelegate(IServiceProvider serviceProvider, ActionExecutor actionExecutor, Type actionProviderType, MethodInfo actionMethod);
    }
}
