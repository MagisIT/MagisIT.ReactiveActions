using System.Collections.Generic;
using System.Threading.Tasks;
using MagisIT.ReactiveActions.Reactivity.Persistence.Models;

namespace MagisIT.ReactiveActions.Reactivity.Persistence
{
    public interface ITrackingSessionStore
    {
        Task StoreTrackedActionCallAsync(string trackingSession, ActionCall actionCall, ICollection<DataQuery> dataQueries);

        Task<DataQuery> GetDataQueryAsync(string trackingSession, string id);

        Task<IEnumerable<DataQuery>> GetDataQueriesForModelAsync(string trackingSession, string modelTypeName);

        Task<IEnumerable<DataQuery>> GetGlobalDataQueriesForModelAsync(string modelTypeName);

        Task<ActionCall> GetActionCallAsync(string trackingSession, string id);

        Task<IEnumerable<ActionCall>> GetActionCallsForActionAsync(string trackingSession, string actionName);

        Task UnregisterSessionAsync(string trackingSession);
    }
}
