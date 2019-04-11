using System.Threading.Tasks;
using MagisIT.ReactiveActions.Reactivity;

namespace MagisIT.ReactiveActions
{
    public delegate Task ActionDelegate(ExecutionContext executionContext, IActionDescriptor actionDescriptor = null);
}
