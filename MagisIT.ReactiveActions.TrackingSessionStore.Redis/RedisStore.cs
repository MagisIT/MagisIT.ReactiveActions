using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MagisIT.ReactiveActions.Reactivity.Persistence;
using MagisIT.ReactiveActions.Reactivity.Persistence.Models;
using MarcusW.SharpUtils.Redis;
using MarcusW.SharpUtils.Redis.Extensions;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace MagisIT.ReactiveActions.TrackingSessionStore.Redis
{
    public partial class RedisStore : ITrackingSessionStore, IDisposable
    {
        private const string RemoveActionCallReferencesScript = "RemoveActionCallReferences.lua";
        private const string RegisterDataQueryReferenceScript = "RegisterDataQueryReference.lua";
        private const string UnregisterSessionScript = "UnregisterSession.lua";

#if DEBUG
        private const Formatting JsonFormatting = Formatting.Indented;
#else
        private const Formatting JsonFormatting = Formatting.None;
#endif

        private readonly IDatabase _redisDatabase;
        private readonly string _prefix;

        private bool _disposed;

        /// <summary>
        /// This key specifies a Redis Set which contains all registered action calls.
        /// Also any action call uses this key as a prefix.
        /// Every registered action call has an additional :refs key for a Redis Set that keeps
        /// track of all data queries referencing to this action call.
        /// </summary>
        private string ActionCallsBaseKey => $"{_prefix}:action-calls";

        /// <summary>
        /// This key specifies a Redis Set which contains all registered data queries.
        /// Also any data query uses this key as a prefix.
        /// </summary>
        private string DataQueriesBaseKey => $"{_prefix}:data-queries";

        /// <summary>
        /// This is a base key for Redis Sets named like PREFIX:model-types:MODEL_TYPE.
        /// Indexing data queries by model type name helps to improve performance significantly.
        /// </summary>
        private string ModelTypesBaseKey => $"{_prefix}:model-types";

        public RedisStore(IDatabase redisDatabase, string prefix)
        {
            _redisDatabase = redisDatabase ?? throw new ArgumentNullException(nameof(redisDatabase));
            _prefix = prefix ?? throw new ArgumentNullException(nameof(prefix));

            // Ensure this store is not used in a clustering setup, because this store is optimized for a single master.
            IConnectionMultiplexer multiplexer = redisDatabase.Multiplexer;
            int mastersCount = multiplexer.GetEndPoints().Count(endpoint => !multiplexer.GetServer(endpoint).IsSlave);
            if (mastersCount > 1)
                throw new NotSupportedException("This redis store cannot be used in clustering setups with multiple masters.");
        }

        public async Task StoreTrackedActionCallAsync(string trackingSession, ActionCall actionCall, ICollection<DataQuery> dataQueries)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(RedisStore));
            if (trackingSession == null)
                throw new ArgumentNullException(nameof(trackingSession));
            if (actionCall == null)
                throw new ArgumentNullException(nameof(actionCall));
            if (dataQueries == null)
                throw new ArgumentNullException(nameof(dataQueries));

            // prefix:action-calls:SESSION:ACTION_CALL_ID
            string actionCallKey = BuildActionCallKey(trackingSession, actionCall.Id);

            // prefix:action-calls:SESSION:ACTION_CALL_ID:refs
            string actionCallReferencesSetKey = $"{actionCallKey}:refs";

            // Serialize action call
            string actionCallJson = JsonConvert.SerializeObject(actionCall, JsonFormatting);

            // Load scripts
            LoadedLuaScript removeActionCallReferencesScript = await GetScriptAsync(RemoveActionCallReferencesScript).ConfigureAwait(false);
            LoadedLuaScript registerDataQueryReferenceScript = await GetScriptAsync(RegisterDataQueryReferenceScript).ConfigureAwait(false);

            // Run as transaction
            ManagedTransaction transaction = _redisDatabase.CreateManagedTransaction();
            {
                // Remove previous references to this action call
                transaction.AddOperation(t => t.ScriptEvaluateAsync(removeActionCallReferencesScript,
                                                                    new {
                                                                        ActionCallId = actionCall.Id,
                                                                        ActionCallReferencesSetKey = (RedisKey)actionCallReferencesSetKey,
                                                                        DataQueriesSetKey = (RedisKey)DataQueriesBaseKey,
                                                                        ModelTypesBaseKey = (RedisKey)ModelTypesBaseKey
                                                                    }));

                // Ensure the action call is set
                transaction.AddOperation(t => t.StringSetAsync(actionCallKey, actionCallJson));
                transaction.AddOperation(t => t.SetAddAsync(ActionCallsBaseKey, actionCallKey));

                // Add/Merge data query references to the action call
                foreach (DataQuery dataQuery in dataQueries)
                {
                    // prefix:data-queries:SESSION:DATA_QUERY_ID
                    string dataQueryKey = BuildDataQueryKey(trackingSession, dataQuery.Id);

                    // Serialize data query
                    string dataQueryJson = JsonConvert.SerializeObject(dataQuery, JsonFormatting);

                    // Add data query
                    transaction.AddOperation(t => t.ScriptEvaluateAsync(registerDataQueryReferenceScript,
                                                                        new {
                                                                            ActionCallId = actionCall.Id,
                                                                            ActionCallReferencesSetKey = (RedisKey)actionCallReferencesSetKey,
                                                                            DataQueriesSetKey = (RedisKey)DataQueriesBaseKey,
                                                                            ModelTypeSetKey = (RedisKey)$"{ModelTypesBaseKey}:{dataQuery.ModelTypeName}",
                                                                            DataQueryKey = (RedisKey)dataQueryKey,
                                                                            DataQueryJson = dataQueryJson
                                                                        }));
                }
            }
            await transaction.ExecuteAsync().ConfigureAwait(false);
        }

        public async Task<DataQuery> GetDataQueryAsync(string trackingSession, string id)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(RedisStore));
            if (trackingSession == null)
                throw new ArgumentNullException(nameof(trackingSession));
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            // Ty to get the data query
            string dataQueryKey = BuildDataQueryKey(trackingSession, id);
            string json = await _redisDatabase.StringGetAsync(dataQueryKey).ConfigureAwait(false);
            if (json == null)
                return null;
            return DeserializeDataQuery(json);
        }

        public Task<IEnumerable<DataQuery>> GetDataQueriesForModelAsync(string trackingSession, string modelTypeName)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(RedisStore));
            if (trackingSession == null)
                throw new ArgumentNullException(nameof(trackingSession));
            if (modelTypeName == null)
                throw new ArgumentNullException(nameof(modelTypeName));

            // All searched keys should have this prefix (See ParameterizedModelFilter.BuildIdentifier())
            string expectedPrefix = $"{DataQueriesBaseKey}:{trackingSession}:{modelTypeName}:";

            // Searching the set that is indexed by model type is faster than searching the set that contains all data queries.
            return GetDataQueriesByModelTypeAsync(modelTypeName, expectedPrefix);
        }

        public Task<IEnumerable<DataQuery>> GetGlobalDataQueriesForModelAsync(string modelTypeName)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(RedisStore));
            if (modelTypeName == null)
                throw new ArgumentNullException(nameof(modelTypeName));

            return GetDataQueriesByModelTypeAsync(modelTypeName);
        }

        public async Task<ActionCall> GetActionCallAsync(string trackingSession, string id)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(RedisStore));
            if (trackingSession == null)
                throw new ArgumentNullException(nameof(trackingSession));
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            // Ty to get the action call
            string actionCallKey = BuildActionCallKey(trackingSession, id);
            string json = await _redisDatabase.StringGetAsync(actionCallKey).ConfigureAwait(false);
            if (json == null)
                return null;
            return DeserializeActionCall(json);
        }

        public async Task<IEnumerable<ActionCall>> GetActionCallsForActionAsync(string trackingSession, string actionName)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(RedisStore));
            if (trackingSession == null)
                throw new ArgumentNullException(nameof(trackingSession));
            if (actionName == null)
                throw new ArgumentNullException(nameof(actionName));

            // TODO: Rewrite this as soon as IAsyncEnumerable is a thing

            // All searched keys should have this prefix (See ActionExecutor.StoreExecutionAsync())
            string expectedPrefix = $"{DataQueriesBaseKey}:{trackingSession}:{actionName}:";

            // Get all registered action calls
            RedisValue[] actionCallKeys = await _redisDatabase.SetMembersAsync(ActionCallsBaseKey).ConfigureAwait(false);
            if (actionCallKeys.Length == 0)
                return Array.Empty<ActionCall>();

            var actionCalls = new List<ActionCall>();
            foreach (string actionCallKey in actionCallKeys)
            {
                // Filter keys
                if (!actionCallKey.StartsWith(expectedPrefix))
                    continue;

                // Try to get the action call. Because we didn't do any locks here, it's not guaranteed that it still exists.
                string actionCallJson = await _redisDatabase.StringGetAsync(actionCallKey).ConfigureAwait(false);
                if (actionCallJson == null)
                    continue;
                ActionCall actionCall = DeserializeActionCall(actionCallJson);

                // Add action call to list
                actionCalls.Add(actionCall);
            }

            return actionCalls;
        }

        public async Task UnregisterSessionAsync(string trackingSession)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(RedisStore));
            if (trackingSession == null)
                throw new ArgumentNullException(nameof(trackingSession));

            // Load and execute script that does this database-internal.
            LoadedLuaScript unregisterSessionScript = await GetScriptAsync(UnregisterSessionScript).ConfigureAwait(false);
            await _redisDatabase.ScriptEvaluateAsync(unregisterSessionScript,
                                                     new {
                                                         ActionCallsSetKey = (RedisKey)ActionCallsBaseKey,
                                                         DataQueriesSetKey = (RedisKey)DataQueriesBaseKey,
                                                         ModelTypesBaseKey = (RedisKey)ModelTypesBaseKey,
                                                         SesionName = trackingSession
                                                     }).ConfigureAwait(false);
        }

        public void Dispose()
        {
            _loadingSemaphore?.Dispose();
            _disposed = true;
        }

        private string BuildDataQueryKey(string trackingSession, string id) => $"{DataQueriesBaseKey}:{trackingSession}:{id}";

        private string BuildActionCallKey(string trackingSession, string id) => $"{ActionCallsBaseKey}:{trackingSession}:{id}";

        private DataQuery DeserializeDataQuery(string json) => JsonConvert.DeserializeObject<DataQuery>(json);

        private ActionCall DeserializeActionCall(string json) => JsonConvert.DeserializeObject<ActionCall>(json);

        private async Task<IEnumerable<DataQuery>> GetDataQueriesByModelTypeAsync(string modelTypeName, string filterKeyPrefix = null, Func<DataQuery, bool> filterFunc = null)
        {
            // TODO: Rewrite this as soon as IAsyncEnumerable is a thing

            // The set that contains all data queries for this model type
            string modelTypeSetKey = $"{ModelTypesBaseKey}:{modelTypeName}";

            // Get all registered data queries for this model type
            RedisValue[] dataQueryKeys = await _redisDatabase.SetMembersAsync(modelTypeSetKey).ConfigureAwait(false);
            if (dataQueryKeys.Length == 0)
                return Array.Empty<DataQuery>();

            var dataQueries = new List<DataQuery>();
            foreach (string dataQueryKey in dataQueryKeys)
            {
                // Filter keys
                if (filterKeyPrefix != null && !dataQueryKey.StartsWith(filterKeyPrefix))
                    continue;

                // Try to get the data query. Because we didn't do any locks here, it's not guaranteed that it still exists.
                string dataQueryJson = await _redisDatabase.StringGetAsync(dataQueryKey).ConfigureAwait(false);
                if (dataQueryJson == null)
                    continue;
                DataQuery dataQuery = DeserializeDataQuery(dataQueryJson);

                // Filter data queries
                if (filterFunc != null && !filterFunc.Invoke(dataQuery))
                    continue;

                // Add data query to list
                dataQueries.Add(dataQuery);
            }

            return dataQueries;
        }
    }
}
