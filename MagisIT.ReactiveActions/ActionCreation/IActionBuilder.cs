using System;
using System.Reflection;

namespace MagisIT.ReactiveActions.ActionCreation
{
    public interface IActionBuilder
    {
        Action BuildAction(IServiceProvider serviceProvider, Type actionProviderType, MethodInfo actionMethod, string actionName);
    }
}
