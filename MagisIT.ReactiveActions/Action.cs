using System;
using System.ComponentModel;
using System.Threading.Tasks;
using MagisIT.ReactiveActions.Reactivity;

namespace MagisIT.ReactiveActions
{
    public delegate Task<object> ActionDelegate(ExecutionContext executionContext, IActionDescriptor actionDescriptor = null);

    [Flags]
    public enum ActionType
    {
        Default = 0,
        Reactive = 1 << 0,
        ReactiveCollection = Reactive | 1 << 1
    }

    public class Action
    {
        public string Name { get; }

        public ActionDelegate ActionDelegate { get; }

        public ActionType Type { get; }

        public bool IsReactive => Type.HasFlag(ActionType.Reactive);

        public bool IsReactiveCollection => Type.HasFlag(ActionType.ReactiveCollection);

        public Action(string name, ActionDelegate actionDelegate, ActionType type)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            ActionDelegate = actionDelegate ?? throw new ArgumentNullException(nameof(actionDelegate));

            if (!Enum.IsDefined(typeof(ActionType), type))
                throw new InvalidEnumArgumentException(nameof(type), (int)type, typeof(ActionType));
            Type = type;
        }

        public Task<object> ExecuteAsync(ExecutionContext executionContext, IActionDescriptor actionDescriptor = null)
        {
            if (executionContext == null)
                throw new ArgumentNullException(nameof(executionContext));

            return ActionDelegate.Invoke(executionContext, actionDescriptor);
        }

        public async Task<TResult> ExecuteAsync<TResult>(ExecutionContext executionContext, IActionDescriptor actionDescriptor = null)
        {
            return (TResult)await ExecuteAsync(executionContext, actionDescriptor).ConfigureAwait(false);
        }
    }
}
