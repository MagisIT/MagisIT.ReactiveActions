using MagisIT.ReactiveActions.Reactivity;

namespace MagisIT.ReactiveActions
{
    public interface IActionProvider
    {
        ExecutionContext ExecutionContext { set; }
    }
}
