namespace MagisIT.ReactiveActions.Sample.ActionProviders.Descriptors
{
    public class GetCartItemActionDescriptor : IActionDescriptor
    {
        public string ProductId { get; set; }

        public string CombinedIdentifier => ProductId;
    }
}
