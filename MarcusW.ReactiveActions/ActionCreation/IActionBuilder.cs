using System;
using System.Reflection;

namespace MarcusW.ReactiveActions.ActionCreation
{
    public interface IActionBuilder
    {
        ActionDelegate BuildActionDelegate(IServiceProvider serviceProvider, ActionExecutor actionExecutor, Type actionProviderType, MethodInfo actionMethod);
    }
}
