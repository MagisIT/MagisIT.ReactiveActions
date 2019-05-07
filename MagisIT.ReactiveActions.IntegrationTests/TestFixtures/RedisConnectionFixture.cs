using System;
using StackExchange.Redis;

namespace MagisIT.ReactiveActions.IntegrationTests.TestFixtures
{
    public class RedisConnectionFixture
    {
        public ConnectionMultiplexer Connection { get; }

        public IDatabase Database => Connection.GetDatabase();

        public RedisConnectionFixture()
        {
            string host = Environment.GetEnvironmentVariable("REDIS_HOST") ?? throw new Exception("REDIS_HOST environment variable missing");
            int port = int.TryParse(Environment.GetEnvironmentVariable("REDIS_PORT"), out int value)
                ? value
                : throw new Exception("REDIS_PORT environment variable invalid or missing");
            string password = Environment.GetEnvironmentVariable("REDIS_PASSWORD") ?? throw new Exception("REDIS_PASSWORD environment variable missing");

            Connection = ConnectionMultiplexer.Connect(new ConfigurationOptions { EndPoints = { { host, port } }, Password = password });
        }
    }
}
