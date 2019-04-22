using System;
using System.Reflection;

namespace MagisIT.ReactiveActions.ActionCreation
{
    public interface IActionDelegateBuilder
    {
        ActionDelegate BuildActionDelegate(IServiceProvider serviceProvider, Type actionProviderType, MethodInfo actionMethod);
    }
}
