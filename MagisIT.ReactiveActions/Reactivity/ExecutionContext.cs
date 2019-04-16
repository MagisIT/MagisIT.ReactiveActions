using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;

namespace MagisIT.ReactiveActions.Reactivity
{
    public class ExecutionContext
    {
        public string TrackingSession { get; }

        public bool TrackingEnabled { get; }

        public ActionExecutor ActionExecutor { get; }

        public Action Action { get; }

        public ParameterizedModelFilter DataQuery { get; private set; }

        private readonly ExecutionContext _parentContext = null;
        private readonly IList<ExecutionContext> _subContexts = new List<ExecutionContext>();

        private ExecutionContext(string trackingSession, bool trackingEnabled, ActionExecutor actionExecutor, Action action)
        {
            TrackingSession = trackingSession;
            TrackingEnabled = trackingEnabled;
            ActionExecutor = actionExecutor;
            Action = action;
        }

        private ExecutionContext(ExecutionContext parentContext, Action action)
        {
            TrackingSession = parentContext.TrackingSession;
            TrackingEnabled = parentContext.TrackingEnabled;
            ActionExecutor = parentContext.ActionExecutor;
            _parentContext = parentContext;
            Action = action;
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
            if (DataQuery != null)
                throw new InvalidOperationException("An action can only register one data query. Please split your action method into multiple actions.");
            if (!modelFilter.AcceptsParameters(filterParams))
                throw new ArgumentException("The model filter doesn't accept the specified filter parameters.");

            DataQuery = new ParameterizedModelFilter(modelFilter, filterParams);
        }

        internal ExecutionContext CreateSubContext(Action action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            var subContext = new ExecutionContext(this, action);
            _subContexts.Add(subContext);
            return subContext;
        }

        internal static ExecutionContext CreateRootContext(string trackingSession, bool trackingEnabled, ActionExecutor actionExecutor, Action action)
        {
            if (trackingSession == null)
                throw new ArgumentNullException(nameof(trackingSession));
            if (actionExecutor == null)
                throw new ArgumentNullException(nameof(actionExecutor));
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            return new ExecutionContext(trackingSession, trackingEnabled, actionExecutor, action);
        }
    }
}
