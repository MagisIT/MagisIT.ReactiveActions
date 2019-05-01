using MagisIT.ReactiveActions.Reactivity;

namespace MagisIT.ReactiveActions
{
    public interface IActionProvider
    {
        IExecutionContext ExecutionContext { set; }
    }
}
