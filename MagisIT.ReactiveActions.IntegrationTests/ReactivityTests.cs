using System;
using System.Linq;
using System.Threading.Tasks;
using MagisIT.ReactiveActions.Attributes;
using MagisIT.ReactiveActions.Helpers;
using MagisIT.ReactiveActions.IntegrationTests.App;
using MagisIT.ReactiveActions.IntegrationTests.App.ActionProviders;
using MagisIT.ReactiveActions.IntegrationTests.App.ActionProviders.Descriptors;
using MagisIT.ReactiveActions.IntegrationTests.App.Models;
using MagisIT.ReactiveActions.IntegrationTests.TestCollections;
using MagisIT.ReactiveActions.IntegrationTests.TestFixtures;
using MagisIT.ReactiveActions.Reactivity.Persistence;
using MagisIT.ReactiveActions.Reactivity.UpdateHandling;
using MagisIT.ReactiveActions.TrackingSessionStore.Redis;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using StackExchange.Redis;
using Xunit;

namespace MagisIT.ReactiveActions.IntegrationTests
{
    [Collection(RedisTestsCollection.Name)]
    public class ReactivityTests
    {
        private readonly RedisConnectionFixture _redisConnectionFixture;

        public ReactivityTests(RedisConnectionFixture redisConnectionFixture)
        {
            _redisConnectionFixture = redisConnectionFixture ?? throw new ArgumentNullException(nameof(redisConnectionFixture));
        }

        [Fact]
        public async Task DetectsResultUpdate()
        {
            var updateHandlerMock = new Mock<IActionResultUpdateHandler>();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IDataSource, StubDataSource>();
            serviceCollection.AddSingleton<IActionBroker>(serviceProvider => {
                ITrackingSessionStore store = new RedisStore(_redisConnectionFixture.Database, $"integration-test:{nameof(DetectsResultUpdate)}:{Guid.NewGuid()}");

                var builder = new ActionBrokerBuilder(serviceProvider, store);
                builder.AddAction<FooActions>(nameof(FooActions.GetFooByIdAsync));
                builder.AddAction<FooActions>(nameof(FooActions.SetFooNameAsync));
                builder.AddModelFilter<Foo, int>(ModelFilters.GetFooByIdFilter);
                builder.AddActionResultUpdateHandler(updateHandlerMock.Object);

                return builder.Build();
            });

            IServiceProvider services = serviceCollection.BuildServiceProvider();

            IActionBroker actionBroker = services.GetRequiredService<IActionBroker>();

            // Should not be detected as an update
            await actionBroker.InvokeActionAsync(nameof(FooActions.SetFooNameAsync), new SetFooNameActionDescriptor { FooId = 0, Name = "Cookie" });
            updateHandlerMock.Verify(handler => handler.HandleResultChangedAsync(It.IsAny<string>(), It.IsAny<Action>(), It.IsAny<IActionDescriptor>()), Times.Never);

            // Query and track this item
            await actionBroker.InvokeAndTrackActionAsync<Foo>("Session1", nameof(FooActions.GetFooByIdAsync), new GetFooByIdActionDescriptor { Id = 0 });

            // This should be detected
            await actionBroker.InvokeActionAsync(nameof(FooActions.SetFooNameAsync), new SetFooNameActionDescriptor { FooId = 0, Name = "Cookie" });
            updateHandlerMock.Verify(handler => handler.HandleResultChangedAsync("Session1",
                                                                                 It.Is<Action>(a => a.Name == nameof(FooActions.GetFooByIdAsync)),
                                                                                 It.Is<GetFooByIdActionDescriptor>(ad => ad.Id == 0)),
                                     Times.Once);
        }
    }
}
