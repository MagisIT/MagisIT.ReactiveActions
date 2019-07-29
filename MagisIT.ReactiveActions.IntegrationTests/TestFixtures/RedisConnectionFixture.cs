using System;
using StackExchange.Redis;

namespace MagisIT.ReactiveActions.IntegrationTests.TestFixtures
{
    public class RedisConnectionFixture
    {
        public const string TestKeysPrefix = "integration-tests";

        public ConnectionMultiplexer Connection { get; }

        public IDatabase Database => Connection.GetDatabase();

        public RedisConnectionFixture()
        {
            string host = Environment.GetEnvironmentVariable("REDIS_HOST") ?? "localhost";
            int port = int.TryParse(Environment.GetEnvironmentVariable("REDIS_PORT"), out int value) ? value : 6379;
            string password = Environment.GetEnvironmentVariable("REDIS_PASSWORD");

            Connection = ConnectionMultiplexer.Connect(new ConfigurationOptions { EndPoints = { { host, port } }, Password = password });
        }
    }
}
