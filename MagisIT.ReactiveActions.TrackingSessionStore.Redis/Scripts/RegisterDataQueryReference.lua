-- Registers a single data query that references an action call
-- WARNING: This script dynamically determines keys and must not be run in a cluster environment!

local action_call_id = @ActionCallId
local action_call_references_set_key = @ActionCallReferencesSetKey
local data_queries_set_key = @DataQueriesSetKey
local model_type_set_key = @ModelTypeSetKey
local data_query_key = @DataQueryKey
local data_query_json = @DataQueryJson

-- Check if the data query already exists
local existing_json = redis.call("GET", data_query_key)
if existing_json then
    -- Get action call references
    local data_query = cjson.decode(data_query_json)
    local action_call_refs = data_query["AffectedActionCalls"]

    -- Get existing action call references
    local existing_data_query = cjson.decode(existing_json)
    local existing_action_call_refs = data_query["AffectedActionCalls"]

    -- Merge new references into existing ones
    for i, ref in ipairs(action_call_refs) do
        -- Search for existing reference for the same action call
        -- (should not exist thanks to the previous cleanup script, but better safe than sorry...)
        local existing = nil
        for j, existing_ref in ipairs(existing_action_call_refs) do
            if existing_ref["ActionCallId"] == ref["ActionCallId"] then
                existing = existing_ref
                break
            end
        end

        -- Does one exist?
        if existing then
            -- Just keep it, but favour indirect references
            if not ref["Direct"] then
                existing["Direct"] = false
            end
        else
            -- Add the new reference
            table.insert(existing_action_call_refs, ref)
        end
    end

    -- Serialize updated data query to json
     data_query_json = cjson.encode(existing_data_query);
end

-- Store data query
redis.call("SET", data_query_key, data_query_json)
redis.call("SADD", data_queries_set_key, data_query_key)
redis.call("SADD", action_call_references_set_key, data_query_key)
redis.call("SADD", model_type_set_key, data_query_key)
