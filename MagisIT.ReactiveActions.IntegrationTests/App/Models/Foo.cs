namespace MagisIT.ReactiveActions.IntegrationTests.App.Models
{
    public class Foo
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public Foo ShallowCopy() => (Foo)MemberwiseClone();
    }
}
