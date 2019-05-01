-- Removes all data queries referencing an action call
-- WARNING: This script dynamically determines keys and must not be run in a cluster environment!

local action_call_id = @ActionCallId
local action_call_references_set_key = @ActionCallReferencesSetKey
local data_queries_set_key = @DataQueriesSetKey
local model_types_base_key = @ModelTypesBaseKey

-- Iterate over all data query keys referencing this action call
local data_query_key
while true do
    -- Get next reference until the end is reached
    data_query_key = redis.call("SPOP", action_call_references_set_key)
    if not data_query_key then break end

    -- Get data query
    local data_query_json = redis.call("GET", data_query_key)
    if data_query_json then
        local data_query = cjson.decode(data_query_json)
        local action_call_refs = data_query["AffectedActionCalls"]

        -- Filter for entries that are not referencing the action call
        local filtered_action_call_refs = {}
        for i, ref in ipairs(action_call_refs) do
            if ref["ActionCallId"] ~= action_call_id then
                table.insert(filtered_action_call_refs, ref)
            end
        end

        -- Are any action call references left?
        if table.getn(filtered_action_call_refs) == 0 then
            -- Just remove the whole entry
            redis.call("DEL", data_query_key)
            redis.call("SREM", data_queries_set_key, data_query_key)

            -- Remove the data query from the model type set
            local model_type_set_key = model_types_base_key .. ":" .. data_query["ModelTypeName"]
            redis.call("SREM", model_type_set_key, data_query_key)
        else
            -- Serialize updated data query to json
            data_query["AffectedActionCalls"] = filtered_action_call_refs
            data_query_json = cjson.encode(data_query)

            -- Write json back
            redis.call("SET", data_query_key, data_query_json)
        end
    end
end
