using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using MagisIT.ReactiveActions.Reactivity;

namespace MagisIT.ReactiveActions.Helpers
{
    public abstract class ActionProviderBase : IActionProvider
    {
        public IExecutionContext ExecutionContext { get; set; }

        protected Task<object> InvokeActionAsync(string name, IActionDescriptor actionDescriptor = null, IActionArguments actionArguments = null)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            return ExecutionContext.ActionExecutor.InvokeSubActionAsync(ExecutionContext, name, actionDescriptor, actionArguments);
        }

        protected Task<TResult> InvokeActionAsync<TResult>(string name, IActionDescriptor actionDescriptor = null, IActionArguments actionArguments = null)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            return ExecutionContext.ActionExecutor.InvokeSubActionAsync<TResult>(ExecutionContext, name, actionDescriptor, actionArguments);
        }

        protected void TrackDataQuery<TModel>(string modelFilterName, params object[] filterParams) where TModel : class
        {
            if (modelFilterName == null)
                throw new ArgumentNullException(nameof(modelFilterName));
            if (filterParams == null)
                throw new ArgumentNullException(nameof(filterParams));

            // Get filter
            ModelFilter modelFilter = ExecutionContext.ActionExecutor.GetModelFilter<TModel>(modelFilterName);

            // Register data query
            ExecutionContext.RegisterDataQuery(modelFilter, filterParams);
        }

        protected TModel TrackEntityQuery<TModel>(TModel queryResult, string modelFilterName, params object[] filterParams) where TModel : class
        {
            if (modelFilterName == null)
                throw new ArgumentNullException(nameof(modelFilterName));
            if (filterParams == null)
                throw new ArgumentNullException(nameof(filterParams));

            // Get filter
            ModelFilter modelFilter = ExecutionContext.ActionExecutor.GetModelFilter<TModel>(modelFilterName);

            // TODO: DEBUG doesn't tell, if the user application but the library is in debug mode
            // Test if the filter matches the result while in debug mode.
            // The queryResult can be null, if the query returned no result. In that case no check is possible.
#if DEBUG
            if (queryResult != null)
            {
                if (!modelFilter.Matches(queryResult, filterParams))
                    throw new ArgumentException("Filter doesn't match query result and might be invalid. This check is only performed in debug mode.");
            }
#endif

            // Register data query
            ExecutionContext.RegisterDataQuery(modelFilter, filterParams);

            return queryResult;
        }

        protected TCollection TrackCollectionQuery<TModel, TCollection>(TCollection queryResult, string modelFilterName, params object[] filterParams)
            where TModel : class where TCollection : ICollection<TModel>
        {
            if (queryResult == null)
                throw new ArgumentNullException(nameof(queryResult));
            if (modelFilterName == null)
                throw new ArgumentNullException(nameof(modelFilterName));
            if (filterParams == null)
                throw new ArgumentNullException(nameof(filterParams));

            // Get filter
            ModelFilter modelFilter = ExecutionContext.ActionExecutor.GetModelFilter<TModel>(modelFilterName);

            // Test if the filter matches all the results while in debug mode.
#if DEBUG
            if (!queryResult.All(resultItem => modelFilter.Matches(resultItem, filterParams)))
                throw new ArgumentException("Filter doesn't match all query results and might be invalid. This check is only performed in debug mode.");

#endif

            // Register data query
            ExecutionContext.RegisterDataQuery(modelFilter, filterParams);

            return queryResult;
        }

        protected ICollection<TModel> TrackCollectionQuery<TModel>(ICollection<TModel> queryResult, string modelFilterName, params object[] filterParams) where TModel : class =>
            TrackCollectionQuery<TModel, ICollection<TModel>>(queryResult, modelFilterName, filterParams);

        protected Task TrackModelUpdateAsync<TModel>(TModel updatedModel, TModel oldModel = null) where TModel : class =>
            ExecutionContext.ActionExecutor.PublishModelUpdateAsync(updatedModel, oldModel);

        protected Task TrackEntityCreatedAsync<TModel>(TModel createdEntity) where TModel : class => TrackModelUpdateAsync(createdEntity);

        protected Task TrackEntityChangedAsync<TModel>(TModel beforeChange, TModel afterChange) where TModel : class => TrackModelUpdateAsync(afterChange, beforeChange);

        protected Task TrackEntityDeletedAsync<TModel>(TModel deletedEntity) where TModel : class => TrackModelUpdateAsync(null, deletedEntity);
    }
}
