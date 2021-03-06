using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using MagisIT.ReactiveActions.ActionCreation;
using MagisIT.ReactiveActions.Attributes;
using MagisIT.ReactiveActions.Helpers;
using MagisIT.ReactiveActions.Reactivity;
using MagisIT.ReactiveActions.Reactivity.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace MagisIT.ReactiveActions.Tests
{
    public class ActionBrokerBuilderTests
    {
        [Fact]
        public void ThrowsWhenServiceProviderNull()
        {
            Assert.Throws<ArgumentNullException>(() => new ActionBrokerBuilder(null, Mock.Of<IActionDelegateBuilder>(), Mock.Of<ITrackingSessionStore>()));
        }

        [Fact]
        public void ThrowsWhenActionDelegateBuilderNull()
        {
            Assert.Throws<ArgumentNullException>(() => new ActionBrokerBuilder(Mock.Of<IServiceProvider>(), null, Mock.Of<ITrackingSessionStore>()));
        }

        [Fact]
        public void ThrowsWhenTrackingSessionStoreNull()
        {
            Assert.Throws<ArgumentNullException>(() => new ActionBrokerBuilder(Mock.Of<IServiceProvider>(), Mock.Of<IActionDelegateBuilder>(), null));
        }

        [Fact]
        public void ThrowsWhenActionMethodNameNull()
        {
            var builder = new ActionBrokerBuilder(Mock.Of<IServiceProvider>(), Mock.Of<ITrackingSessionStore>());
            Assert.Throws<ArgumentNullException>(() => builder.AddAction<TestActions>(null));
        }

        [Fact]
        public void ThrowsWhenActionMethodNameNotExists()
        {
            var builder = new ActionBrokerBuilder(Mock.Of<IServiceProvider>(), Mock.Of<IActionDelegateBuilder>(), Mock.Of<ITrackingSessionStore>());
            Assert.Throws<ArgumentException>(() => builder.AddAction<TestActions>("invalid"));
        }

        [Fact]
        public void ThrowsWhenActionNotAsync()
        {
            var builder = new ActionBrokerBuilder(Mock.Of<IServiceProvider>(), Mock.Of<IActionDelegateBuilder>(), Mock.Of<ITrackingSessionStore>());
            Assert.Throws<ArgumentException>(() => builder.AddAction<TestActions>(nameof(TestActions.SynchronousAction)));
        }

        [Fact]
        public void ThrowsWhenActionAttributeMissing()
        {
            var builder = new ActionBrokerBuilder(Mock.Of<IServiceProvider>(), Mock.Of<IActionDelegateBuilder>(), Mock.Of<ITrackingSessionStore>());
            Assert.Throws<ArgumentException>(() => builder.AddAction<TestActions>(nameof(TestActions.ActionWithoutAttributeAsync)));
        }

        [Theory]
        [InlineData(nameof(TestActions.SimpleActionAsync), ActionType.Default, typeof(bool))]
        [InlineData(nameof(TestActions.ReactiveActionAsync), ActionType.Reactive, typeof(TestModel))]
        [InlineData(nameof(TestActions.ReactiveCollectionActionAsync), ActionType.ReactiveCollection, typeof(ICollection<TestModel>))]
        public void RegistersActionMethodOnce(string actionMethodName, ActionType expectedType, Type expectedResultType)
        {
            IServiceProvider serviceProvider = Mock.Of<IServiceProvider>();
            Mock<IActionDelegateBuilder> actionDelegateBuilderMock = new Mock<IActionDelegateBuilder>(MockBehavior.Strict);

            Expression<Func<IActionDelegateBuilder, ActionDelegate>> expectedBuilderInvocation = actionDelegateBuilder =>
                actionDelegateBuilder.BuildActionDelegate(serviceProvider, typeof(TestActions), It.Is<MethodInfo>(info => info.Name == actionMethodName));
            actionDelegateBuilderMock.Setup(expectedBuilderInvocation).Returns((executionContext, actionDescriptor, actionArguments) => Task.FromResult<object>(null));

            var builder = new ActionBrokerBuilder(serviceProvider, actionDelegateBuilderMock.Object, Mock.Of<ITrackingSessionStore>());

            // Register action the first time
            builder.AddAction<TestActions>(actionMethodName);
            Assert.Contains(builder.Actions, a => a.Key == actionMethodName && a.Value != null);

            // Delegate builder should have been called exactly once
            actionDelegateBuilderMock.Verify(expectedBuilderInvocation, Times.Once);

            // Verify action
            Action action = builder.Actions[actionMethodName];
            Assert.Equal(actionMethodName, action.Name);
            Assert.Equal(expectedType, action.Type);
            Assert.Equal(expectedResultType, action.ResultType);

            // Doing it a second time should fail
            Assert.Throws<ArgumentException>(() => builder.AddAction<TestActions>(actionMethodName));
        }

        [Fact]
        public void ThrowsWhenReactiveActionHasNoResult()
        {
            var builder = new ActionBrokerBuilder(Mock.Of<IServiceProvider>(), Mock.Of<IActionDelegateBuilder>(), Mock.Of<ITrackingSessionStore>());
            Assert.Throws<ArgumentException>(() => builder.AddAction<TestActions>(nameof(TestActions.ReactiveActionWithoutResultAsync)));
        }

        [Fact]
        public void ThrowsWhenReactiveCollectionActionReturnsNoCollection()
        {
            var builder = new ActionBrokerBuilder(Mock.Of<IServiceProvider>(), Mock.Of<IActionDelegateBuilder>(), Mock.Of<ITrackingSessionStore>());
            Assert.Throws<ArgumentException>(() => builder.AddAction<TestActions>(nameof(TestActions.ReactiveCollectionActionWithoutCollectionAsync)));
        }

        [Fact]
        public void RegistersModelFilterOnce()
        {
            var builder = new ActionBrokerBuilder(Mock.Of<IServiceProvider>(), Mock.Of<IActionDelegateBuilder>(), Mock.Of<ITrackingSessionStore>());
            builder.AddModelFilter<TestModel, int>(TestFilters.TestFilter);

            Assert.Contains(builder.ModelFilters, f => f.Key == nameof(TestFilters.TestFilter));

            ModelFilter filter = builder.ModelFilters[nameof(TestFilters.TestFilter)];
            Assert.Equal(nameof(TestFilters.TestFilter), filter.Name);
            Assert.Equal(typeof(TestModel), filter.ModelType);

            Assert.Throws<ArgumentException>(() => builder.AddModelFilter<TestModel, int>(TestFilters.TestFilter));
        }

        [Fact]
        public void ThrowsIfModelFilterIsAnonymousMethod()
        {
            var builder = new ActionBrokerBuilder(Mock.Of<IServiceProvider>(), Mock.Of<IActionDelegateBuilder>(), Mock.Of<ITrackingSessionStore>());
            Assert.Throws<ArgumentException>(() => builder.AddModelFilter<TestModel, int>((model, id) => model.Id == id));
        }

        [Fact]
        public void ThrowsIfFilterIsNotStatic()
        {
            var nonStaticFilters = new NonStaticTestFilters();

            var builder = new ActionBrokerBuilder(Mock.Of<IServiceProvider>(), Mock.Of<IActionDelegateBuilder>(), Mock.Of<ITrackingSessionStore>());
            Assert.Throws<ArgumentException>(() => builder.AddModelFilter<TestModel, int>(nonStaticFilters.TestFilter));
        }

        [Fact]
        public void ThrowsIfFilterParameterIsComplex()
        {
            var builder = new ActionBrokerBuilder(Mock.Of<IServiceProvider>(), Mock.Of<IActionDelegateBuilder>(), Mock.Of<ITrackingSessionStore>());
            Assert.Throws<ArgumentException>(() => builder.AddModelFilter<TestModel, object>(TestFilters.FilterWithComplexParameter));
        }

        [Fact]
        public void ThrowsIfFilterParameterIsOptional()
        {
            var builder = new ActionBrokerBuilder(Mock.Of<IServiceProvider>(), Mock.Of<IActionDelegateBuilder>(), Mock.Of<ITrackingSessionStore>());
            Assert.Throws<ArgumentException>(() => builder.AddModelFilter<TestModel, string>(TestFilters.FilterWithOptionalParameter));
        }

        private class TestActions : ActionProviderBase
        {
            [Action]
            public Task<bool> SimpleActionAsync()
            {
                return Task.FromResult(true);
            }

            [Action, Reactive]
            public Task<TestModel> ReactiveActionAsync()
            {
                return Task.FromResult(new TestModel());
            }

            [Action, ReactiveCollection]
            public Task<ICollection<TestModel>> ReactiveCollectionActionAsync()
            {
                return Task.FromResult<ICollection<TestModel>>(Array.Empty<TestModel>());
            }

            [Action, Reactive]
            public Task ReactiveActionWithoutResultAsync()
            {
                return Task.CompletedTask;
            }

            [Action, ReactiveCollection]
            public Task<TestModel> ReactiveCollectionActionWithoutCollectionAsync()
            {
                return Task.FromResult(new TestModel());
            }

            [Action]
            public void SynchronousAction() { }

            public Task ActionWithoutAttributeAsync()
            {
                return Task.CompletedTask;
            }
        }

        private class TestModel
        {
            public int Id { get; set; }
        }

        private static class TestFilters
        {
            public static bool TestFilter(TestModel entity, int id) => entity.Id == id;

            public static bool FilterWithComplexParameter(TestModel entity, object someObject) => entity == someObject;

            public static bool FilterWithOptionalParameter(TestModel entity, string someString = null) => entity.Id.ToString() == someString;
        }

        private class NonStaticTestFilters
        {
            public bool TestFilter(TestModel entity, int id) => entity.Id == id;
        }
    }
}
