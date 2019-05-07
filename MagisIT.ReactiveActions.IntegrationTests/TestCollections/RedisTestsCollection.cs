using MagisIT.ReactiveActions.IntegrationTests.TestFixtures;
using Xunit;

namespace MagisIT.ReactiveActions.IntegrationTests.TestCollections
{
    [CollectionDefinition(Name)]
    public class RedisTestsCollection : ICollectionFixture<RedisConnectionFixture>
    {
        public const string Name = "Redis Tests";
    }
}
