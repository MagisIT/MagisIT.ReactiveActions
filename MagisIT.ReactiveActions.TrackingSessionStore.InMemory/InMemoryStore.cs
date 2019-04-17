using System;
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
    public class InMemoryStore : ITrackingSessionStore, IDisposable
    {
        private readonly SemaphoreSlim _storeSemaphore = new SemaphoreSlim(1, 1);

        private readonly IDictionary<string, TrackingSessionEntry> _trackingSessionEntries = new Dictionary<string, TrackingSessionEntry>();

        private bool _disposed;

        public async Task StoreTrackedActionCallAsync(string trackingSession, ActionCall actionCall, ICollection<DataQuery> dataQueries)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(InMemoryStore));
            if (trackingSession == null)
                throw new ArgumentNullException(nameof(trackingSession));
            if (actionCall == null)
                throw new ArgumentNullException(nameof(actionCall));
            if (dataQueries == null)
                throw new ArgumentNullException(nameof(dataQueries));

            await _storeSemaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                // Get/Create session entry
                TrackingSessionEntry sessionEntry;
                if (_trackingSessionEntries.ContainsKey(trackingSession))
                    sessionEntry = _trackingSessionEntries[trackingSession];
                else
                    _trackingSessionEntries.Add(trackingSession, sessionEntry = new TrackingSessionEntry(trackingSession));

                // Add action call
                sessionEntry.AddActionCall(actionCall, dataQueries);
            }
            finally
            {
                _storeSemaphore.Release();
            }
        }

        public async Task<DataQuery> GetDataQueryAsync(string trackingSession, string id)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(InMemoryStore));
            if (trackingSession == null)
                throw new ArgumentNullException(nameof(trackingSession));
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            await _storeSemaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                return _trackingSessionEntries.ContainsKey(trackingSession) ? _trackingSessionEntries[trackingSession].GetDataQuery(id) : null;
            }
            finally
            {
                _storeSemaphore.Release();
            }
        }

        public async Task<IEnumerable<DataQuery>> GetDataQueriesForModelAsync(string trackingSession, string modelTypeName)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(InMemoryStore));
            if (trackingSession == null)
                throw new ArgumentNullException(nameof(trackingSession));
            if (modelTypeName == null)
                throw new ArgumentNullException(nameof(modelTypeName));

            await _storeSemaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                return _trackingSessionEntries.ContainsKey(trackingSession)
                    ? _trackingSessionEntries[trackingSession].GetDataQueriesForModel(modelTypeName)
                    : Enumerable.Empty<DataQuery>();
            }
            finally
            {
                _storeSemaphore.Release();
            }
        }

        public async Task<ActionCall> GetActionCallAsync(string trackingSession, string id)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(InMemoryStore));
            if (trackingSession == null)
                throw new ArgumentNullException(nameof(trackingSession));
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            await _storeSemaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                return _trackingSessionEntries.ContainsKey(trackingSession) ? _trackingSessionEntries[trackingSession].GetActionCall(id) : null;
            }
            finally
            {
                _storeSemaphore.Release();
            }
        }

        public async Task<IEnumerable<ActionCall>> GetActionCallsForActionAsync(string trackingSession, string actionName)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(InMemoryStore));
            if (trackingSession == null)
                throw new ArgumentNullException(nameof(trackingSession));
            if (actionName == null)
                throw new ArgumentNullException(nameof(actionName));

            await _storeSemaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                return _trackingSessionEntries.ContainsKey(trackingSession)
                    ? _trackingSessionEntries[trackingSession].GetActionCallsForAction(actionName)
                    : Enumerable.Empty<ActionCall>();
            }
            finally
            {
                _storeSemaphore.Release();
            }
        }

        public async Task UnregisterSessionAsync(string trackingSession)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(InMemoryStore));
            if (trackingSession == null)
                throw new ArgumentNullException(nameof(trackingSession));

            await _storeSemaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                _trackingSessionEntries.Remove(trackingSession);
            }
            finally
            {
                _storeSemaphore.Release();
            }
        }

        public void Dispose()
        {
            _storeSemaphore?.Dispose();
            _disposed = true;
        }

        private class TrackingSessionEntry
        {
            public string Name { get; }

            private readonly IDictionary<string, ActionCall> _actionCalls = new Dictionary<string, ActionCall>();
            private readonly IDictionary<string, DataQuery> _dataQueries = new Dictionary<string, DataQuery>();

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
                    // Merge affected action calls with existing ones
                    if (_dataQueries.ContainsKey(dataQuery.Id))
                    {
                        DataQuery existingDataQuery = _dataQueries[dataQuery.Id];
                        existingDataQuery.AffectedActionCalls = existingDataQuery.AffectedActionCalls.Union(dataQuery.AffectedActionCalls).ToArray();
                        continue;
                    }

                    // Add data query
                    _dataQueries.Add(dataQuery.Id, dataQuery);
                }
            }

            public DataQuery GetDataQuery(string id) => _dataQueries.TryGetValue(id, out DataQuery dataQuery) ? dataQuery : null;

            public IEnumerable<DataQuery> GetDataQueriesForModel(string modelTypeName) => _dataQueries.Values.Where(dataQuery => dataQuery.ModelTypeName == modelTypeName);

            public ActionCall GetActionCall(string id) => _actionCalls.TryGetValue(id, out ActionCall actionCall) ? actionCall : null;

            public IEnumerable<ActionCall> GetActionCallsForAction(string actionName) => _actionCalls.Values.Where(actionCall => actionCall.ActionName == actionName);
        }
    }
}
