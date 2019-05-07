namespace MagisIT.ReactiveActions.IntegrationTests.App.ActionProviders.Descriptors
{
    public class GetFooByIdActionDescriptor : IActionDescriptor
    {
        public int Id { get; set; }

        public string CombinedIdentifier => Id.ToString();
    }
}
