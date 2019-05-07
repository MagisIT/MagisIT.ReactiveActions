namespace MagisIT.ReactiveActions.IntegrationTests.App.ActionProviders.Descriptors
{
    public class SetFooNameActionDescriptor : IActionDescriptor
    {
        public int FooId { get; set; }

        public string Name { get; set; }

        public string CombinedIdentifier => $"{FooId}:{Name}";
    }
}
