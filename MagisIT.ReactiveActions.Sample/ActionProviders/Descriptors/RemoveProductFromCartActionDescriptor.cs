namespace MagisIT.ReactiveActions.Sample.ActionProviders.Descriptors
{
    public class RemoveProductFromCartActionDescriptor : IActionDescriptor
    {
        public string ProductId { get; set; }

        public string CombinedIdentifier => ProductId;
    }
}
