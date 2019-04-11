using System.Threading.Tasks;

namespace MagisIT.ReactiveActions.Reactivity.UpdateHandling
{
    public interface IActionResultUpdateHandler
    {
        Task HandleResultChangedAsync<TActionDescriptor>(ActionBroker actoinBroker, string actionName, TActionDescriptor actionDescriptor)
            where TActionDescriptor : IActionDescriptor;

        Task HandleResultItemChangedAsync<TModel, TActionDescriptor>(ActionBroker actoinBroker, string actionName, TActionDescriptor actionDescriptor, TModel item)
            where TActionDescriptor : IActionDescriptor;
    }
}
