using System;
using System.Linq;
using System.Text;

namespace MagisIT.ReactiveActions.Reactivity
{
    public class ParameterizedModelFilter
    {
        public ModelFilter ModelFilter { get; }

        public object[] FilterParams { get; }

        public string Identifier => _identifier ?? (_identifier = BuildIdentifier());

        private string _identifier;

        internal ParameterizedModelFilter(ModelFilter modelFilter, object[] filterParams)
        {
            ModelFilter = modelFilter ?? throw new ArgumentNullException(nameof(modelFilter));
            FilterParams = filterParams ?? throw new ArgumentNullException(nameof(filterParams));
        }

        private string BuildIdentifier()
        {
            string parameters = string.Join(":", FilterParams.Select(p => p.ToString()));
            return $"{ModelFilter.FullName}:{parameters}";
        }
    }
}
