using System;
using System.Threading.Tasks;
using MagisIT.ReactiveActions.Attributes;
using MagisIT.ReactiveActions.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MagisIT.ReactiveActions.Tests
{
    public class ActionBrokerBuilderTests
    {
        private readonly ServiceProvider _serviceProvider;
        private readonly ActionBrokerBuilder _builder;

        public ActionBrokerBuilderTests()
        {
            _serviceProvider = new ServiceCollection().BuildServiceProvider();
            _builder = new ActionBrokerBuilder(_serviceProvider);
        }

        [Fact]
        public void ThrowsWhenServiceProviderNull()
        {
            Assert.Throws<ArgumentNullException>(() => new ActionBrokerBuilder(null));
        }

        [Fact]
        public void ThrowsWhenActionBuilderNull()
        {
            Assert.Throws<ArgumentNullException>(() => new ActionBrokerBuilder(_serviceProvider, null));
        }

        [Fact]
        public void ThrowsWhenActionMethodNameNull()
        {
            Assert.Throws<ArgumentNullException>(() => _builder.AddAction<TestActions>(null));
        }

        [Fact]
        public void ThrowsWhenActionMethodNameNotExists()
        {
            Assert.Throws<ArgumentException>(() => _builder.AddAction<TestActions>("invalid"));
        }

        [Fact]
        public void ActionMethodIsRegistered()
        {
            _builder.AddAction<TestActions>(nameof(TestActions.SimpleActionAsync));
            Assert.Contains(_builder.ActionExecutor.Actions, a => a.Key == nameof(TestActions.SimpleActionAsync) && a.Value != null);
        }

        [Fact]
        public void ThrowsWhenActionMethodAlreadyRegistered()
        {
            _builder.AddAction<TestActions>(nameof(TestActions.SimpleActionAsync));
            Assert.Throws<ArgumentException>(() => _builder.AddAction<TestActions>(nameof(TestActions.SimpleActionAsync)));
        }

        [Fact]
        public void ThrowsWhenActionNotAsync()
        {
            Assert.Throws<ArgumentException>(() => _builder.AddAction<TestActions>(nameof(TestActions.SynchronousAction)));
        }

        [Fact]
        public void ThrowsWhenActionAttributeMissing()
        {
            Assert.Throws<ArgumentException>(() => _builder.AddAction<TestActions>(nameof(TestActions.ActionWithoutAttributeAsync)));
        }

        private class TestActions : ActionProviderBase
        {
            [Action]
            public Task SimpleActionAsync()
            {
                return Task.CompletedTask;
            }

            [Action]
            public void SynchronousAction() { }

            public Task ActionWithoutAttributeAsync()
            {
                return Task.CompletedTask;
            }
        }
    }
}
