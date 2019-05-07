using System;
using System.Linq;
using System.Threading.Tasks;
using MagisIT.ReactiveActions.Attributes;
using MagisIT.ReactiveActions.Helpers;
using MagisIT.ReactiveActions.IntegrationTests.App.ActionProviders.Descriptors;
using MagisIT.ReactiveActions.IntegrationTests.App.Models;

namespace MagisIT.ReactiveActions.IntegrationTests.App.ActionProviders
{
    public class FooActions : ActionProviderBase
    {
        [Action, Reactive]
        public Task<Foo> GetFooByIdAsync(IDataSource dataSource, GetFooByIdActionDescriptor actionDescriptor)
        {
            Foo foo = TrackEntityQuery(dataSource.Foos.FirstOrDefault(f => f.Id == actionDescriptor.Id), nameof(ModelFilters.GetFooByIdFilter), actionDescriptor.Id);
            return Task.FromResult(foo);
        }

        [Action]
        public async Task SetFooNameAsync(IDataSource dataSource, SetFooNameActionDescriptor actionDescriptor)
        {
            Foo foo = dataSource.Foos.FirstOrDefault(f => f.Id == actionDescriptor.FooId)
                      ?? throw new ArgumentException($"Foo {actionDescriptor.FooId} does not exist", nameof(actionDescriptor));

            Foo previousFoo = foo.ShallowCopy();
            foo.Name = actionDescriptor.Name;
            await TrackEntityChangedAsync(previousFoo, foo).ConfigureAwait(false);
        }
    }
}
