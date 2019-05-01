using System;
using System.Reflection;
using System.Threading.Tasks;
using MagisIT.ReactiveActions.ActionCreation;
using MagisIT.ReactiveActions.Attributes;
using MagisIT.ReactiveActions.Helpers;
using MagisIT.ReactiveActions.Reactivity;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace MagisIT.ReactiveActions.Tests
{
    public class ReflectionActionDelegateBuilderTests
    {
        [Fact]
        public void ThrowsWhenServiceProviderNull()
        {
            Assert.Throws<ArgumentNullException>(
                () => new ReflectionActionDelegateBuilder().BuildActionDelegate(null, typeof(TestActions), typeof(TestActions).GetMethod(nameof(TestActions.SimpleActionAsync))));
        }

        [Fact]
        public void ThrowsWhenActionProviderTypeNull()
        {
            Assert.Throws<ArgumentNullException>(
                () => new ReflectionActionDelegateBuilder().BuildActionDelegate(Mock.Of<IServiceProvider>(),
                                                                                null,
                                                                                typeof(TestActions).GetMethod(nameof(TestActions.SimpleActionAsync))));
        }

        [Fact]
        public void ThrowsWhenActionMethodNull()
        {
            Assert.Throws<ArgumentNullException>(() => new ReflectionActionDelegateBuilder().BuildActionDelegate(Mock.Of<IServiceProvider>(), typeof(TestActions), null));
        }

        [Fact]
        public Task ThrowsWhenNoExecutionContextGiven()
        {
            MethodInfo actionMethod = typeof(TestActions).GetMethod(nameof(TestActions.SimpleActionAsync));
            ActionDelegate actionDelegate = new ReflectionActionDelegateBuilder().BuildActionDelegate(Mock.Of<IServiceProvider>(), typeof(TestActions), actionMethod);
            return Assert.ThrowsAsync<ArgumentNullException>(() => actionDelegate.Invoke(null));
        }

        [Fact]
        public Task DoesNotRequireActionDescriptorWhenNotRequired()
        {
            MethodInfo actionMethod = typeof(TestActions).GetMethod(nameof(TestActions.SimpleActionAsync));
            ActionDelegate actionDelegate = new ReflectionActionDelegateBuilder().BuildActionDelegate(Mock.Of<IServiceProvider>(), typeof(TestActions), actionMethod);
            return actionDelegate.Invoke(Mock.Of<IExecutionContext>());
        }

        [Fact]
        public Task ThrowsWhenNoActionDescriptorGiven()
        {
            MethodInfo actionMethod = typeof(TestActions).GetMethod(nameof(TestActions.ActionWithActionDescriptorAsync));
            ActionDelegate actionDelegate = new ReflectionActionDelegateBuilder().BuildActionDelegate(Mock.Of<IServiceProvider>(), typeof(TestActions), actionMethod);
            return Assert.ThrowsAsync<ArgumentNullException>(() => actionDelegate.Invoke(Mock.Of<IExecutionContext>()));
        }

        [Theory]
        [InlineData(nameof(TestActions.SimpleActionAsync))]
        [InlineData(nameof(TestActions.ActionWithSomeDependencyAsync))]
        public Task ThrowsWhenNoActionDescriptorRequired(string actionMethodName)
        {
            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock.Setup(provider => provider.GetService(It.Is<Type>(type => type == typeof(SomeDependency)))).Returns(new SomeDependency());

            MethodInfo actionMethod = typeof(TestActions).GetMethod(actionMethodName);
            ActionDelegate actionDelegate = new ReflectionActionDelegateBuilder().BuildActionDelegate(serviceProviderMock.Object, typeof(TestActions), actionMethod);
            return Assert.ThrowsAsync<ArgumentException>(() => actionDelegate.Invoke(Mock.Of<IExecutionContext>(), new SomeActionDescriptor()));
        }

        [Fact]
        public Task ThrowsWhenActionDescriptorHasWrongType()
        {
            MethodInfo actionMethod = typeof(TestActions).GetMethod(nameof(TestActions.ActionWithActionDescriptorAsync));
            ActionDelegate actionDelegate = new ReflectionActionDelegateBuilder().BuildActionDelegate(Mock.Of<IServiceProvider>(), typeof(TestActions), actionMethod);
            return Assert.ThrowsAsync<ArgumentException>(() => actionDelegate.Invoke(Mock.Of<IExecutionContext>(), new OtherActionDescriptor()));
        }

        [Fact]
        public async Task ResolvesDependency()
        {
            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock.Setup(provider => provider.GetService(It.Is<Type>(type => type == typeof(SomeDependency)))).Returns(new SomeDependency()).Verifiable();

            MethodInfo actionMethod = typeof(TestActions).GetMethod(nameof(TestActions.ActionWithSomeDependencyAsync));
            ActionDelegate actionDelegate = new ReflectionActionDelegateBuilder().BuildActionDelegate(serviceProviderMock.Object, typeof(TestActions), actionMethod);
            await actionDelegate.Invoke(Mock.Of<IExecutionContext>());

            serviceProviderMock.Verify();
        }

        [Fact]
        public async Task ThrowsWhenRequiredDependencyCannotBeResolved()
        {
            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock.Setup(provider => provider.GetService(It.Is<Type>(type => type == typeof(SomeDependency)))).Returns((SomeDependency)null).Verifiable();

            MethodInfo actionMethod = typeof(TestActions).GetMethod(nameof(TestActions.ActionWithSomeDependencyAsync));
            ActionDelegate actionDelegate = new ReflectionActionDelegateBuilder().BuildActionDelegate(serviceProviderMock.Object, typeof(TestActions), actionMethod);
            await Assert.ThrowsAsync<InvalidOperationException>(() => actionDelegate.Invoke(Mock.Of<IExecutionContext>()));

            serviceProviderMock.Verify();
        }

        [Fact]
        public async Task IgnoresOptionalDependencyThatCannotBeResolved()
        {
            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock.Setup(provider => provider.GetService(It.Is<Type>(type => type == typeof(SomeDependency)))).Returns((SomeDependency)null).Verifiable();

            MethodInfo actionMethod = typeof(TestActions).GetMethod(nameof(TestActions.ActionWithOptionalDependencyAsync));
            ActionDelegate actionDelegate = new ReflectionActionDelegateBuilder().BuildActionDelegate(serviceProviderMock.Object, typeof(TestActions), actionMethod);
            await actionDelegate.Invoke(Mock.Of<IExecutionContext>());

            serviceProviderMock.Verify();
        }

        private class SomeDependency { }

        private class SomeActionDescriptor : IActionDescriptor
        {
            public string Id { get; set; }

            public string CombinedIdentifier => Id;
        }

        private class OtherActionDescriptor : IActionDescriptor
        {
            public string Id { get; set; }

            public string CombinedIdentifier => Id;
        }

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
            public Task ActionWithOptionalDependencyAsync(SomeDependency otherDependency = null)
            {
                return Task.CompletedTask;
            }
        }
    }
}
