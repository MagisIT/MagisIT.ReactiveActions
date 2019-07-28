-- Unregisters a single tracking session and all data queries and action calls belonging to it
-- WARNING: This script dynamically determines keys and must not be run in a cluster environment!

local action_calls_set_key = @ActionCallsSetKey
local data_queries_set_key = @DataQueriesSetKey
local model_types_base_key = @ModelTypesBaseKey
local session_name = @SessionName

-- Remove all data queries for this tracking session
local data_query_session_prefix = data_queries_set_key .. ":" .. session_name .. ":"
local data_query_keys = redis.call("SMEMBERS", data_queries_set_key)
for i, data_query_key in ipairs(data_query_keys) do
    -- Is it dedicated to the current session?
    if data_query_key:sub(1, #data_query_session_prefix) == data_query_session_prefix then
        -- Get data query
        local data_query_json = redis.call("GET", data_query_key)
        if data_query_json then
            local data_query = cjson.decode(data_query_json)

            -- Delete data query entry
            redis.call("DEL", data_query_key)
            redis.call("SREM", data_queries_set_key, data_query_key)

            -- Remove the data query from the model type set
            local model_type_set_key = model_types_base_key .. ":" .. data_query["ModelTypeName"]
            redis.call("SREM", model_type_set_key, data_query_key)
        end
    end
end

-- Remove all action calls for this tracking session_name
local action_call_session_prefix = action_calls_set_key .. ":" .. session_name .. ":"
local action_call_keys = redis.call("SMEMBERS", data_queries_set_key)
for i, action_call_key in ipairs(data_query_keys) do
    -- Is it dedicated to the current session?
    if action_call_key:sub(1, #action_call_session_prefix) == action_call_session_prefix then
        -- Delete action call entry and references set
        redis.call("DEL", action_call_key)
        redis.call("DEL", action_call_key .. ":refs")
        redis.call("SREM", action_calls_set_key, action_call_key)
    end
end
