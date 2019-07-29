using System.Collections.Generic;

namespace MagisIT.ReactiveActions.Reactivity
{
    public interface IExecutionContext
    {
        IActionExecutor ActionExecutor { get; }

        Action Action { get; }

        string TrackingSession { get; }

        bool TrackingEnabled { get; }

        IList<IExecutionContext> SubContexts { get; }

        IList<(ParameterizedModelFilter filter, bool isResultSource)> DataQueries { get; }

        void RegisterDataQuery(bool isResultSource, ModelFilter modelFilter, object[] filterParams);

        IExecutionContext CreateSubContext(Action action);
    }
}
