namespace MagisIT.ReactiveActions.Reactivity.Persistence.Models
{
    public sealed class ActionCall
    {
        public string TrackingSession { get; set; }

        public string Id { get; set; }

        public string ActionName { get; set; }

        public string ActionDescriptorTypeName { get; set; }

        public IActionDescriptor ActionDescriptor { get; set; }
    }
}
