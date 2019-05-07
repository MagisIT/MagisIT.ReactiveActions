namespace MagisIT.ReactiveActions.IntegrationTests.App.Models
{
    public class Bar
    {
        public int Id { get; set; }

        public int Happiness { get; set; }

        public bool TastesGood { get; set; }

        public Bar ShallowCopy() => (Bar)MemberwiseClone();
    }
}
