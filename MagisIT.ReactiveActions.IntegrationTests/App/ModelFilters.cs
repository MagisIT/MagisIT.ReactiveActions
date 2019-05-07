using MagisIT.ReactiveActions.IntegrationTests.App.Models;

namespace MagisIT.ReactiveActions.IntegrationTests.App
{
    public static class ModelFilters
    {
        public static bool GetFooByIdFilter(Foo foo, int id) => foo.Id == id;
    }
}
