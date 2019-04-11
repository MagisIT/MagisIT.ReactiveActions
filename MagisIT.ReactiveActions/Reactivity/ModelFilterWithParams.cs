using System;

namespace MagisIT.ReactiveActions.Reactivity
{
    public class ModelFilterWithParams
    {
        public ModelFilter ModelFilter { get; }

        public object[] FilterParams { get; }

        internal ModelFilterWithParams(ModelFilter modelFilter, object[] filterParams)
        {
            ModelFilter = modelFilter ?? throw new ArgumentNullException(nameof(modelFilter));
            FilterParams = filterParams ?? throw new ArgumentNullException(nameof(filterParams));
        }
    }
}
