namespace MagisIT.ReactiveActions.Reactivity.Persistence.Models
{
    public sealed class DataQuery
    {
        public string TrackingSession { get; set; }

        public string Id { get; set; }

        public string ModelTypeName { get; set; }

        public string FilterName { get; set; }

        public object[] FilterParams { get; set; }

        public ActionCallReference[] AffectedActionCalls { get; set; }
    }
}
