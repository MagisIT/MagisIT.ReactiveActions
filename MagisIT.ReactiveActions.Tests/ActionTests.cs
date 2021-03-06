using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using MagisIT.ReactiveActions.Attributes;
using MagisIT.ReactiveActions.Helpers;
using Xunit;

namespace MagisIT.ReactiveActions.Tests
{
    public class ActionTests
    {
        [Theory]
        [InlineData(nameof(TestActions.SimpleActionAsync), ActionType.Default, null, false, false)]
        [InlineData(nameof(TestActions.SimpleActionWithResultAsync), ActionType.Default, typeof(int), false, false)]
        [InlineData(nameof(TestActions.ReactiveActionAsync), ActionType.Reactive, typeof(TestModel), true, false)]
        [InlineData(nameof(TestActions.ReactiveCollectionActionAsync), ActionType.ReactiveCollection, typeof(ICollection<TestModel>), true, true)]
        public void InterpretsTypeCorrectly(string actionMethodName, ActionType type, Type resultType, bool shouldBeReactive, bool shouldBeReactiveCollection)
        {
            MethodInfo methodInfo = typeof(TestActions).GetMethod(actionMethodName);
            var action = new Action(actionMethodName, (context, descriptor, arguments) => Task.FromResult<object>(null), methodInfo, type, resultType);
            Assert.Equal(shouldBeReactive, action.IsReactive);
            Assert.Equal(shouldBeReactiveCollection, action.IsReactiveCollection);
        }

        private class TestActions : ActionProviderBase
        {
            [Action]
            public Task SimpleActionAsync()
            {
                return Task.CompletedTask;
            }

            [Action]
            public Task<int> SimpleActionWithResultAsync()
            {
                return Task.FromResult(42);
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
        }

        private class TestModel { }
    }
}
