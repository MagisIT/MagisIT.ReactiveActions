using System.Threading.Tasks;

namespace MagisIT.ReactiveActions.Reactivity.UpdateHandling
{
    public interface IActionResultUpdateHandler
    {
        Task HandleResultChangedAsync(string trackingSession, Action action, IActionDescriptor actionDescriptor);

        Task HandleResultItemChangedAsync<TModel>(string trackingSession, Action action, IActionDescriptor actionDescriptor, TModel itemBeforeUpdate, TModel itemAfterUpdate)
            where TModel : class;
    }
}
