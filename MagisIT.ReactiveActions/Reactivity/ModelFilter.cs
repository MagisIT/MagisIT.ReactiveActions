using System;
using System.Linq;
using System.Reflection;

namespace MagisIT.ReactiveActions.Reactivity
{
    public class ModelFilter
    {
        public Type ModelType { get; }

        public string Name { get; }

        public Delegate FilterDelegate { get; }

        public string FullName { get; }

        private readonly ParameterInfo[] _requiredParameters;

        internal ModelFilter(Type modelType, string name, Delegate filterDelegate)
        {
            ModelType = modelType ?? throw new ArgumentNullException(nameof(modelType));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            FilterDelegate = filterDelegate ?? throw new ArgumentNullException(nameof(filterDelegate));

            FullName = $"{modelType.Name}:{Name}";

            _requiredParameters = filterDelegate.Method.GetParameters().Skip(1).ToArray();
        }

        public bool CanFilterModelType(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            return ModelType.IsAssignableFrom(type);
        }

        public bool AcceptsParameters(object[] filterParams)
        {
            if (filterParams == null)
                throw new ArgumentNullException(nameof(filterParams));

            if (filterParams.Length != _requiredParameters.Length)
                return false;

            for (int i = 0; i < _requiredParameters.Length; i++)
            {
                ParameterInfo parameter = _requiredParameters[i];
                object value = filterParams[i];

                if (value == null && !parameter.IsOptional)
                    return false;
                if (!parameter.ParameterType.IsInstanceOfType(value))
                    return false;
            }

            return true;
        }

        public bool Matches<TModel>(TModel entity, params object[] filterParams)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));
            if (filterParams == null)
                throw new ArgumentNullException(nameof(filterParams));
            if (!CanFilterModelType(typeof(TModel)))
                throw new ArgumentException("Model type is incompatible to this model filter.", nameof(TModel));

            object[] parameters = filterParams.Prepend(entity).ToArray();
            return (bool)FilterDelegate.DynamicInvoke(parameters);
        }
    }
}
