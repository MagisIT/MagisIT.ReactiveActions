using System.Threading.Tasks;
using MagisIT.ReactiveActions.Reactivity;

namespace MagisIT.ReactiveActions
{
    public interface IActionExecutor
    {
        Task<object> InvokeActionAsync(string name, IActionDescriptor actionDescriptor = null, string trackingSession = null);

        Task<TResult> InvokeActionAsync<TResult>(string name, IActionDescriptor actionDescriptor = null, string trackingSession = null);

        Task<object> InvokeSubActionAsync(IExecutionContext currentExecutionContext, string name, IActionDescriptor actionDescriptor = null);

        Task<TResult> InvokeSubActionAsync<TResult>(IExecutionContext currentExecutionContext, string name, IActionDescriptor actionDescriptor = null);

        ModelFilter GetModelFilter(string name);

        ModelFilter GetModelFilter<TModel>(string name) where TModel : class;

        Task PublishModelUpdateAsync<TModel>(TModel updatedModel, TModel oldModel = null) where TModel : class;
    }
}
