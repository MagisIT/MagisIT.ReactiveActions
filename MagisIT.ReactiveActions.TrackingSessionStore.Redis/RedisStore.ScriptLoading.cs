using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace MagisIT.ReactiveActions.TrackingSessionStore.Redis
{
    public partial class RedisStore
    {
        private readonly IDictionary<string, LoadedLuaScript> _loadedScripts = new Dictionary<string, LoadedLuaScript>();
        private readonly SemaphoreSlim _loadingSemaphore = new SemaphoreSlim(1, 1);

        private async Task<LoadedLuaScript> GetScriptAsync(string scriptName)
        {
            // Get master server
            IServer masterServer = _redisDatabase.Multiplexer.GetEndPoints().Select(endpoint => _redisDatabase.Multiplexer.GetServer(endpoint)).First(server => !server.IsSlave);

            await _loadingSemaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_loadedScripts.ContainsKey(scriptName))
                    return _loadedScripts[scriptName];

                // Get script from resource file
                string script;
                using (Stream resourceStream = typeof(RedisStore).Assembly.GetManifestResourceStream(scriptName))
                using (var streamReader = new StreamReader(resourceStream ?? throw new ArgumentException("Script could not be loaded.", nameof(scriptName))))
                    script = await streamReader.ReadToEndAsync().ConfigureAwait(false);

                // Load script
                LuaScript preparedScript = LuaScript.Prepare(script);
                LoadedLuaScript loadedScript = await preparedScript.LoadAsync(masterServer).ConfigureAwait(false);
                _loadedScripts[scriptName] = loadedScript;

                // Return loaded script
                return loadedScript;
            }
            finally
            {
                _loadingSemaphore.Release();
            }
        }
    }
}
