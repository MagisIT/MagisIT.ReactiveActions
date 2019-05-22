using System.Threading.Tasks;

namespace MagisIT.ReactiveActions
{
    public interface IActionBroker
    {
        Task<object> InvokeAndTrackActionAsync(string trackingSession, string name, IActionDescriptor actionDescriptor = null, IActionArguments actionArguments = null);

        Task<TResult> InvokeAndTrackActionAsync<TResult>(string trackingSession, string name, IActionDescriptor actionDescriptor = null, IActionArguments actionArguments = null);

        Task<object> InvokeActionAsync(string name, IActionDescriptor actionDescriptor = null, IActionArguments actionArguments = null);

        Task<TResult> InvokeActionAsync<TResult>(string name, IActionDescriptor actionDescriptor = null, IActionArguments actionArguments = null);
    }
}
