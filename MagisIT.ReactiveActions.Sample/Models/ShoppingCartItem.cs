namespace MagisIT.ReactiveActions.Sample.Models
{
    public class ShoppingCartItem
    {
        public string ProductId { get; set; }

        public int Amount { get; set; }

        public ShoppingCartItem ShallowCopy() => (ShoppingCartItem)MemberwiseClone();
    }
}
