using System.Threading.Tasks;

namespace MagisIT.ReactiveActions
{
    public interface IActionBroker
    {
        Task<object> InvokeAndTrackActionAsync(string trackingSession, string name, IActionDescriptor actionDescriptor = null);

        Task<TResult> InvokeAndTrackActionAsync<TResult>(string trackingSession, string name, IActionDescriptor actionDescriptor = null);

        Task<object> InvokeActionAsync(string name, IActionDescriptor actionDescriptor = null);

        Task<TResult> InvokeActionAsync<TResult>(string name, IActionDescriptor actionDescriptor = null);
    }
}
