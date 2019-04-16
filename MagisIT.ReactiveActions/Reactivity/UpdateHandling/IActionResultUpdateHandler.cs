using System.Threading.Tasks;

namespace MagisIT.ReactiveActions.Reactivity.UpdateHandling
{
    public interface IActionResultUpdateHandler
    {
        Task HandleResultChangedAsync<TActionDescriptor>(string trackingSession, Action action, TActionDescriptor actionDescriptor) where TActionDescriptor : IActionDescriptor;

        Task HandleResultItemChangedAsync<TModel, TActionDescriptor>(string trackingSession, Action action, TActionDescriptor actionDescriptor, TModel item)
            where TActionDescriptor : IActionDescriptor;
    }
}
