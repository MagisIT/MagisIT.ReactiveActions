using System;
using System.ComponentModel;
using System.Reflection;
using System.Threading.Tasks;
using MagisIT.ReactiveActions.Reactivity;

namespace MagisIT.ReactiveActions
{
    public delegate Task<object> ActionDelegate(IExecutionContext executionContext, IActionDescriptor actionDescriptor = null);

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

        public MethodInfo ActionMethodInfo { get; }

        public ActionType Type { get; }

        public Type ResultType { get; }

        public Type ResultModelType { get; }

        public bool IsReactive => Type.HasFlag(ActionType.Reactive);

        public bool IsReactiveCollection => Type.HasFlag(ActionType.ReactiveCollection);

        public Action(string name, ActionDelegate actionDelegate, MethodInfo actionMethodInfo, ActionType type, Type resultType, Type resultModelType)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            ActionDelegate = actionDelegate ?? throw new ArgumentNullException(nameof(actionDelegate));
            ActionMethodInfo = actionMethodInfo ?? throw new ArgumentNullException(nameof(actionMethodInfo));

            if (!Enum.IsDefined(typeof(ActionType), type))
                throw new InvalidEnumArgumentException(nameof(type), (int)type, typeof(ActionType));
            Type = type;

            if (IsReactive)
            {
                ResultModelType = ResultType = resultType ?? throw new ArgumentException("The result type of reactive actions must be known.", nameof(resultType));

                if (IsReactiveCollection)
                    ResultModelType = resultModelType
                                      ?? throw new ArgumentException("The result model type of reactive-collection actions must be known.", nameof(resultModelType));
            }
        }

        public Task<object> ExecuteAsync(IExecutionContext executionContext, IActionDescriptor actionDescriptor = null)
        {
            if (executionContext == null)
                throw new ArgumentNullException(nameof(executionContext));

            return ActionDelegate.Invoke(executionContext, actionDescriptor);
        }

        public async Task<TResult> ExecuteAsync<TResult>(IExecutionContext executionContext, IActionDescriptor actionDescriptor = null)
        {
            return (TResult)await ExecuteAsync(executionContext, actionDescriptor).ConfigureAwait(false);
        }
    }
}
