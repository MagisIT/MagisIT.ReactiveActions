using System;
using System.Reflection;
using MagisIT.ReactiveActions.Reactivity;
using Xunit;

namespace MagisIT.ReactiveActions.Tests
{
    public class ModelFilterTests
    {
        [Fact]
        public void AcceptsModelType()
        {
            Func<TestModel, int, bool> filterDelegate = TestFilters.TestFilter;
            var modelFilter = new ModelFilter(typeof(TestModel), filterDelegate.Method.Name, filterDelegate);

            Assert.True(modelFilter.CanFilterModelType(typeof(TestModel)));
        }

        [Fact]
        public void AcceptsDerivedModelType()
        {
            Func<TestModel, int, bool> filterDelegate = TestFilters.TestFilter;
            var modelFilter = new ModelFilter(typeof(TestModel), filterDelegate.Method.Name, filterDelegate);

            Assert.True(modelFilter.CanFilterModelType(typeof(DerivedModel)));
        }

        [Theory]
        [InlineData(42)]
        [InlineData((uint)42)]
        [InlineData((long)42)]
        [InlineData((ulong)42)]
        [InlineData((short)42)]
        [InlineData((ushort)42)]
        [InlineData((float)42)]
        [InlineData((double)42)]
        public void AcceptsCompatibleParameters(object numericParameter)
        {
            Func<TestModel, int, bool> filterDelegate = TestFilters.TestFilter;
            var modelFilter = new ModelFilter(typeof(TestModel), filterDelegate.Method.Name, filterDelegate);

            Assert.True(modelFilter.AcceptsParameters(new[] { numericParameter }));
        }

        [Theory]
        [InlineData(long.MaxValue)]
        [InlineData(typeof(int))]
        [InlineData("test")]
        public void RejectsIncompatibleParameters(object parameter)
        {
            Func<TestModel, int, bool> filterDelegate = TestFilters.TestFilter;
            var modelFilter = new ModelFilter(typeof(TestModel), filterDelegate.Method.Name, filterDelegate);

            Assert.False(modelFilter.AcceptsParameters(new[] { parameter }));
        }

        private class TestModel
        {
            public int Id { get; set; }
        }

        private class DerivedModel : TestModel { }

        private static class TestFilters
        {
            public static bool TestFilter(TestModel entity, int id) => entity.Id == id;
        }
    }
}
