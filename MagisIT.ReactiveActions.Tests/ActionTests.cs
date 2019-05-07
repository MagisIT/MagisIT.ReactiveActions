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
        [InlineData(nameof(TestActions.SimpleActionAsync), ActionType.Default, null, null, false, false)]
        [InlineData(nameof(TestActions.ReactiveActionAsync), ActionType.Reactive, typeof(TestModel), typeof(TestModel), true, false)]
        [InlineData(nameof(TestActions.ReactiveCollectionActionAsync), ActionType.ReactiveCollection, typeof(ICollection<TestModel>), typeof(TestModel), true, true)]
        public void InterpretsTypeCorrectly(string actionMethodName, ActionType type, Type resultType, Type resultModelType, bool shouldBeReactive, bool shouldBeReactiveCollection)
        {
            MethodInfo methodInfo = typeof(TestActions).GetMethod(actionMethodName);
            var action = new Action(actionMethodName, (context, descriptor) => Task.FromResult<object>(null), methodInfo, type, resultType, resultModelType);
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
