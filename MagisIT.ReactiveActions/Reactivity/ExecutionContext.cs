using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace MagisIT.ReactiveActions.Reactivity
{
    public class ExecutionContext
    {
        public ActionExecutor ActionExecutor { get; }

        public ParameterizedModelFilter DataQuery { get; private set; }

        private readonly ExecutionContext _parentContext = null;
        private readonly IList<ExecutionContext> _subContexts = new List<ExecutionContext>();

        private ExecutionContext(ActionExecutor actionExecutor)
        {
            ActionExecutor = actionExecutor;
        }

        private ExecutionContext(ExecutionContext parentContext)
        {
            ActionExecutor = parentContext.ActionExecutor;
            _parentContext = parentContext;
        }

        public void RegisterDataQuery(ModelFilter modelFilter, object[] filterParams)
        {
            if (modelFilter == null)
                throw new ArgumentNullException(nameof(modelFilter));
            if (filterParams == null)
                throw new ArgumentNullException(nameof(filterParams));
            if (DataQuery != null)
                throw new InvalidOperationException("An action can only register one data query. Please split your action method into multiple actions.");
            if (!modelFilter.AcceptsParameters(filterParams))
                throw new ArgumentException("The model filter doesn't accept the specified filter parameters.");

            DataQuery = new ParameterizedModelFilter(modelFilter, filterParams);
        }

        internal ExecutionContext CreateSubContext()
        {
            var subContext = new ExecutionContext(this);
            _subContexts.Add(subContext);
            return subContext;
        }

        internal static ExecutionContext CreateRootContext(ActionExecutor actionExecutor)
        {
            return new ExecutionContext(actionExecutor);
        }
    }
}
