namespace MagisIT.ReactiveActions.Sample.ActionProviders.Descriptors
{
    public class AddProductToCartActionDescriptor : IActionDescriptor
    {
        public string ProductId { get; set; }

        public int Amount { get; set; }

        public string CombinedIdentifier => $"{ProductId}:{Amount}";
    }
}
