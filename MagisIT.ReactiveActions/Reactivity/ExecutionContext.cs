using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace MagisIT.ReactiveActions.Reactivity
{
    public class ExecutionContext : IExecutionContext
    {
        public IActionExecutor ActionExecutor { get; }

        public Action Action { get; }


        public string TrackingSession { get; }

        public bool TrackingEnabled => TrackingSession != null;

        public IList<IExecutionContext> SubContexts { get; } = new List<IExecutionContext>();

        public IList<ParameterizedModelFilter> DataQueries { get; } = new List<ParameterizedModelFilter>();

        private ExecutionContext(IActionExecutor actionExecutor, Action action, string trackingSession = null)
        {
            ActionExecutor = actionExecutor;
            Action = action;
            TrackingSession = trackingSession;
        }

        private ExecutionContext(ExecutionContext parentContext, Action action)
        {
            ActionExecutor = parentContext.ActionExecutor;
            Action = action;
            TrackingSession = parentContext.TrackingSession;
        }

        public void RegisterDataQuery(ModelFilter modelFilter, object[] filterParams)
        {
            if (modelFilter == null)
                throw new ArgumentNullException(nameof(modelFilter));
            if (filterParams == null)
                throw new ArgumentNullException(nameof(filterParams));

            if (!TrackingEnabled)
                return;

            if (!Action.IsReactive)
                throw new InvalidOperationException("Action methods that are not marked as reactive cannot register data queries.");
            if (!modelFilter.AcceptsParameters(filterParams))
                throw new ArgumentException("The model filter doesn't accept the specified filter parameters.");

            DataQueries.Add(new ParameterizedModelFilter(modelFilter, filterParams));
        }

        public IExecutionContext CreateSubContext(Action action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            var subContext = new ExecutionContext(this, action);
            SubContexts.Add(subContext);
            return subContext;
        }

        public static ExecutionContext CreateRootContext(IActionExecutor actionExecutor, Action action, string trackingSession = null)
        {
            if (actionExecutor == null)
                throw new ArgumentNullException(nameof(actionExecutor));
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            return new ExecutionContext(actionExecutor, action, trackingSession);
        }
    }
}
