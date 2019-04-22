using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using MagisIT.ReactiveActions.Reactivity;
using MagisIT.ReactiveActions.Reactivity.Persistence;
using MagisIT.ReactiveActions.Reactivity.Persistence.Models;

namespace MagisIT.ReactiveActions.TrackingSessionStore.InMemory
{
    public class InMemoryStore : ITrackingSessionStore
    {
        private readonly ConcurrentDictionary<string, TrackingSessionEntry> _trackingSessionEntries = new ConcurrentDictionary<string, TrackingSessionEntry>();

        public Task StoreTrackedActionCallAsync(string trackingSession, ActionCall actionCall, ICollection<DataQuery> dataQueries)
        {
            if (trackingSession == null)
                throw new ArgumentNullException(nameof(trackingSession));
            if (actionCall == null)
                throw new ArgumentNullException(nameof(actionCall));
            if (dataQueries == null)
                throw new ArgumentNullException(nameof(dataQueries));

            // Get/Create session entry
            TrackingSessionEntry sessionEntry = _trackingSessionEntries.GetOrAdd(trackingSession, session => new TrackingSessionEntry(session));

            // Add action call
            sessionEntry.AddActionCall(actionCall, dataQueries);

            return Task.CompletedTask;
        }

        public Task<DataQuery> GetDataQueryAsync(string trackingSession, string id)
        {
            if (trackingSession == null)
                throw new ArgumentNullException(nameof(trackingSession));
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            return Task.FromResult(_trackingSessionEntries.TryGetValue(trackingSession, out TrackingSessionEntry sessionEntry) ? sessionEntry.GetDataQuery(id) : null);
        }

        public Task<IEnumerable<DataQuery>> GetDataQueriesForModelAsync(string trackingSession, string modelTypeName)
        {
            if (trackingSession == null)
                throw new ArgumentNullException(nameof(trackingSession));
            if (modelTypeName == null)
                throw new ArgumentNullException(nameof(modelTypeName));

            return Task.FromResult(_trackingSessionEntries.TryGetValue(trackingSession, out TrackingSessionEntry sessionEntry)
                                       ? sessionEntry.GetDataQueriesForModel(modelTypeName)
                                       : null);
        }

        public Task<IEnumerable<DataQuery>> GetGlobalDataQueriesForModelAsync(string modelTypeName)
        {
            if (modelTypeName == null)
                throw new ArgumentNullException(nameof(modelTypeName));

            return Task.FromResult(_trackingSessionEntries.SelectMany(sessionPair => sessionPair.Value.GetDataQueriesForModel(modelTypeName)));
        }

        public Task<ActionCall> GetActionCallAsync(string trackingSession, string id)
        {
            if (trackingSession == null)
                throw new ArgumentNullException(nameof(trackingSession));
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            return Task.FromResult(_trackingSessionEntries.TryGetValue(trackingSession, out TrackingSessionEntry sessionEntry) ? sessionEntry.GetActionCall(id) : null);
        }

        public Task<IEnumerable<ActionCall>> GetActionCallsForActionAsync(string trackingSession, string actionName)
        {
            if (trackingSession == null)
                throw new ArgumentNullException(nameof(trackingSession));
            if (actionName == null)
                throw new ArgumentNullException(nameof(actionName));

            return Task.FromResult(_trackingSessionEntries.TryGetValue(trackingSession, out TrackingSessionEntry sessionEntry)
                                       ? sessionEntry.GetActionCallsForAction(actionName)
                                       : null);
        }

        public Task UnregisterSessionAsync(string trackingSession)
        {
            if (trackingSession == null)
                throw new ArgumentNullException(nameof(trackingSession));

            _trackingSessionEntries.TryRemove(trackingSession, out TrackingSessionEntry _);
            return Task.CompletedTask;
        }

        private class TrackingSessionEntry
        {
            public string Name { get; }

            private readonly ConcurrentDictionary<string, ActionCall> _actionCalls = new ConcurrentDictionary<string, ActionCall>();
            private readonly ConcurrentDictionary<string, DataQuery> _dataQueries = new ConcurrentDictionary<string, DataQuery>();

            public TrackingSessionEntry(string name)
            {
                Name = name;
            }

            public void AddActionCall(ActionCall actionCall, ICollection<DataQuery> dataQueries)
            {
                // Ensure the action call is set
                _actionCalls[actionCall.Id] = actionCall;

                // Iterate through all registered data queries and remove any references to this action call
                foreach (DataQuery dataQuery in _dataQueries.Values)
                    dataQuery.AffectedActionCalls = dataQuery.AffectedActionCalls.Where(c => c.ActionCallId != actionCall.Id).ToArray();

                // Add/Merge data query references
                foreach (DataQuery dataQuery in dataQueries)
                {
                    // Add data query
                    _dataQueries.AddOrUpdate(dataQuery.Id,
                                             dataQuery,
                                             (id, existingDataQuery) => {
                                                 // Merge affected action calls with existing ones
                                                 // This might be called multiple times when there was a threading conflict, so this call must not be destructive.
                                                 existingDataQuery.AffectedActionCalls = existingDataQuery.AffectedActionCalls.Union(dataQuery.AffectedActionCalls).ToArray();
                                                 return existingDataQuery;
                                             });
                }
            }

            public DataQuery GetDataQuery(string id) => _dataQueries.TryGetValue(id, out DataQuery dataQuery) ? dataQuery : null;

            public IEnumerable<DataQuery> GetDataQueriesForModel(string modelTypeName) => _dataQueries.Values.Where(dataQuery => dataQuery.ModelTypeName == modelTypeName);

            public ActionCall GetActionCall(string id) => _actionCalls.TryGetValue(id, out ActionCall actionCall) ? actionCall : null;

            public IEnumerable<ActionCall> GetActionCallsForAction(string actionName) => _actionCalls.Values.Where(actionCall => actionCall.ActionName == actionName);
        }
    }
}
