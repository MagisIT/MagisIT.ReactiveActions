using System;
using System.Reflection;
using System.Threading.Tasks;
using MarcusW.ReactiveActions.ActionCreation;
using MarcusW.ReactiveActions.Attributes;
using MarcusW.ReactiveActions.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MarcusW.ReactiveActions.Tests
{
    public class ReflectionActionBuilderTests
    {
        private readonly ReflectionActionBuilder _actionBuilder;
        private readonly ServiceProvider _serviceProvider;
        private readonly ActionExecutor _actionExecutor;

        public ReflectionActionBuilderTests()
        {
            _actionBuilder = new ReflectionActionBuilder();
            _serviceProvider = new ServiceCollection().AddSingleton<SomeDependency>().BuildServiceProvider();
            _actionExecutor = new ActionExecutor();
        }

        [Fact]
        public void ThrowsWhenServiceProviderNull()
        {
            Assert.Throws<ArgumentNullException>(() => _actionBuilder.BuildActionDelegate(null, null, null, null));
        }

        [Fact]
        public void ThrowsWhenActionExecutorNull()
        {
            Assert.Throws<ArgumentNullException>(() => _actionBuilder.BuildActionDelegate(_serviceProvider, null, null, null));
        }

        [Fact]
        public void ThrowsWhenActionProviderTypeNull()
        {
            Assert.Throws<ArgumentNullException>(() => _actionBuilder.BuildActionDelegate(_serviceProvider, _actionExecutor, null, null));
        }

        [Fact]
        public void ThrowsWhenActionMethodNull()
        {
            Assert.Throws<ArgumentNullException>(() => _actionBuilder.BuildActionDelegate(_serviceProvider, _actionExecutor, typeof(TestActions), null));
        }

        [Theory]
        [InlineData(nameof(TestActions.SimpleActionAsync))]
        [InlineData(nameof(TestActions.ActionWithSomeDependencyAsync))]
        public Task DoesNotRequireActionDescriptorWhenNotRequired(string actionMethodName)
        {
            MethodInfo actionMethod = typeof(TestActions).GetMethod(actionMethodName);
            ActionDelegate actionDelegate = _actionBuilder.BuildActionDelegate(_serviceProvider, _actionExecutor, typeof(TestActions), actionMethod);
            return actionDelegate.Invoke();
        }

        [Fact]
        public Task ThrowsWhenNoActionDescriptorGiven()
        {
            MethodInfo actionMethod = typeof(TestActions).GetMethod(nameof(TestActions.ActionWithActionDescriptorAsync));
            ActionDelegate actionDelegate = _actionBuilder.BuildActionDelegate(_serviceProvider, _actionExecutor, typeof(TestActions), actionMethod);
            return Assert.ThrowsAsync<ArgumentNullException>(() => actionDelegate.Invoke());
        }

        [Theory]
        [InlineData(nameof(TestActions.SimpleActionAsync))]
        [InlineData(nameof(TestActions.ActionWithSomeDependencyAsync))]
        public Task ThrowsWhenNoActionDescriptorRequired(string actionMethodName)
        {
            MethodInfo actionMethod = typeof(TestActions).GetMethod(actionMethodName);
            ActionDelegate actionDelegate = _actionBuilder.BuildActionDelegate(_serviceProvider, _actionExecutor, typeof(TestActions), actionMethod);
            return Assert.ThrowsAsync<ArgumentException>(() => actionDelegate.Invoke(new SomeActionDescriptor()));
        }

        [Fact]
        public Task ThrowsWhenActionDescriptorHasWrongType()
        {
            MethodInfo actionMethod = typeof(TestActions).GetMethod(nameof(TestActions.ActionWithActionDescriptorAsync));
            ActionDelegate actionDelegate = _actionBuilder.BuildActionDelegate(_serviceProvider, _actionExecutor, typeof(TestActions), actionMethod);
            return Assert.ThrowsAsync<ArgumentException>(() => actionDelegate.Invoke(new OtherActionDescriptor()));
        }

        [Fact]
        public Task ResolvesDependency()
        {
            MethodInfo actionMethod = typeof(TestActions).GetMethod(nameof(TestActions.ActionWithSomeDependencyAsync));
            ActionDelegate actionDelegate = _actionBuilder.BuildActionDelegate(_serviceProvider, _actionExecutor, typeof(TestActions), actionMethod);
            return actionDelegate.Invoke();
        }

        [Fact]
        public Task IgnoresOptionalDependencyThatCannotBeResolved()
        {
            MethodInfo actionMethod = typeof(TestActions).GetMethod(nameof(TestActions.ActionWithOptionalOtherDependencyAsync));
            ActionDelegate actionDelegate = _actionBuilder.BuildActionDelegate(_serviceProvider, _actionExecutor, typeof(TestActions), actionMethod);
            return actionDelegate.Invoke();
        }

        [Fact]
        public Task ThrowsWhenRequiredDependencyCannotBeResolved()
        {
            MethodInfo actionMethod = typeof(TestActions).GetMethod(nameof(TestActions.ActionWithOtherDependencyAsync));
            ActionDelegate actionDelegate = _actionBuilder.BuildActionDelegate(_serviceProvider, _actionExecutor, typeof(TestActions), actionMethod);
            return Assert.ThrowsAsync<InvalidOperationException>(() => actionDelegate.Invoke());
        }

        private class SomeDependency { }

        private class OtherDependency { }

        private class SomeActionDescriptor : IActionDescriptor { }

        private class OtherActionDescriptor : IActionDescriptor { }

        private class TestActions : ActionProviderBase
        {
            [Action]
            public Task SimpleActionAsync()
            {
                return Task.CompletedTask;
            }

            [Action]
            public Task ActionWithActionDescriptorAsync(SomeActionDescriptor actionDescriptor)
            {
                return Task.CompletedTask;
            }

            [Action]
            public Task ActionWithSomeDependencyAsync(SomeDependency someDependency)
            {
                return Task.CompletedTask;
            }

            [Action]
            public Task ActionWithOtherDependencyAsync(OtherDependency otherDependency)
            {
                return Task.CompletedTask;
            }

            [Action]
            public Task ActionWithOptionalOtherDependencyAsync(OtherDependency otherDependency = null)
            {
                return Task.CompletedTask;
            }
        }
    }
}
