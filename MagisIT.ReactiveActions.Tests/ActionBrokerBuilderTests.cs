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
        [InlineData(nameof(TestActions.SimpleActionAsync), ActionType.Default, null, null)]
        [InlineData(nameof(TestActions.ReactiveActionAsync), ActionType.Reactive, typeof(TestModel), typeof(TestModel))]
        [InlineData(nameof(TestActions.ReactiveCollectionActionAsync), ActionType.ReactiveCollection, typeof(ICollection<TestModel>), typeof(TestModel))]
        public void RegistersActionMethodOnce(string actionMethodName, ActionType expectedType, Type expectedResultType, Type expectedResultModelType)
        {
            IServiceProvider serviceProvider = Mock.Of<IServiceProvider>();
            Mock<IActionDelegateBuilder> actionDelegateBuilderMock = new Mock<IActionDelegateBuilder>(MockBehavior.Strict);

            Expression<Func<IActionDelegateBuilder, ActionDelegate>> expectedBuilderInvocation = actionDelegateBuilder =>
                actionDelegateBuilder.BuildActionDelegate(serviceProvider, typeof(TestActions), It.Is<MethodInfo>(info => info.Name == actionMethodName));
            actionDelegateBuilderMock.Setup(expectedBuilderInvocation).Returns((executionContext, actionDescriptor) => Task.FromResult<object>(null));

            var builder = new ActionBrokerBuilder(serviceProvider, actionDelegateBuilderMock.Object, Mock.Of<ITrackingSessionStore>());

            // Register action the first time
            builder.AddAction<TestActions>(actionMethodName);
            Assert.Contains(builder.Actions, a => a.Key == actionMethodName && a.Value != null);

            // Delegate builder should have been called exactly once
            actionDelegateBuilderMock.Verify(expectedBuilderInvocation, Times.Once);

            // Verify action
            Action action = builder.Actions[actionMethodName];
            Assert.Equal(action.Name, actionMethodName);
            Assert.Equal(action.Type, expectedType);
            Assert.Equal(action.ResultType, expectedResultType);
            Assert.Equal(action.ResultModelType, expectedResultModelType);

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
            Assert.Equal(filter.Name, nameof(TestFilters.TestFilter));
            Assert.Equal(filter.ModelType, typeof(TestModel));

            Assert.Throws<ArgumentException>(() => builder.AddModelFilter<TestModel, int>(TestFilters.TestFilter));
        }

        [Fact]
        public void ThrowsIfModelFilterIsAnonymousMethod()
        {
            var builder = new ActionBrokerBuilder(Mock.Of<IServiceProvider>(), Mock.Of<IActionDelegateBuilder>(), Mock.Of<ITrackingSessionStore>());
            Assert.Throws<ArgumentException>(() => builder.AddModelFilter<TestModel, int>((model, id) => model.Id == id));
        }

        [Fact]
        public void ThrowsIfFilterParameterIsComplex()
        {
            var builder = new ActionBrokerBuilder(Mock.Of<IServiceProvider>(), Mock.Of<IActionDelegateBuilder>(), Mock.Of<ITrackingSessionStore>());
            Assert.Throws<ArgumentException>(() => builder.AddModelFilter<TestModel, object>(TestFilters.FilterWithComplexParameter));
        }

        private class TestActions : ActionProviderBase
        {
            [Action]
            public Task SimpleActionAsync()
            {
                return Task.CompletedTask;
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
        }
    }
}
