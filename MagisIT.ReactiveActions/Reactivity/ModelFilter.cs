using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace MagisIT.ReactiveActions.Reactivity
{
    public class ModelFilter
    {
        private static readonly Dictionary<Type, Func<Expression, Expression>> ValueConverters = new Dictionary<Type, Func<Expression, Expression>> {
            // Thanks to the use of nameof() we are sure the selected methods exist and therefore null checks are not necessary.
            // ReSharper disable AssignNullToNotNullAttribute
            { typeof(sbyte), value => Expression.Call(null, typeof(Convert).GetMethod(nameof(Convert.ToSByte), new[] { typeof(object) }), value) },
            { typeof(short), value => Expression.Call(null, typeof(Convert).GetMethod(nameof(Convert.ToInt16), new[] { typeof(object) }), value) },
            { typeof(int), value => Expression.Call(null, typeof(Convert).GetMethod(nameof(Convert.ToInt32), new[] { typeof(object) }), value) },
            { typeof(long), value => Expression.Call(null, typeof(Convert).GetMethod(nameof(Convert.ToInt64), new[] { typeof(object) }), value) },
            { typeof(byte), value => Expression.Call(null, typeof(Convert).GetMethod(nameof(Convert.ToByte), new[] { typeof(object) }), value) },
            { typeof(ushort), value => Expression.Call(null, typeof(Convert).GetMethod(nameof(Convert.ToUInt16), new[] { typeof(object) }), value) },
            { typeof(uint), value => Expression.Call(null, typeof(Convert).GetMethod(nameof(Convert.ToUInt32), new[] { typeof(object) }), value) },
            { typeof(ulong), value => Expression.Call(null, typeof(Convert).GetMethod(nameof(Convert.ToUInt64), new[] { typeof(object) }), value) },
            { typeof(float), value => Expression.Call(null, typeof(Convert).GetMethod(nameof(Convert.ToSingle), new[] { typeof(object) }), value) },
            { typeof(double), value => Expression.Call(null, typeof(Convert).GetMethod(nameof(Convert.ToDouble), new[] { typeof(object) }), value) }
            // ReSharper restore AssignNullToNotNullAttribute
        };

        public Type ModelType { get; }

        public string Name { get; }

        public Delegate FilterDelegate { get; }

        public string FullName { get; }

        private Func<object[], bool> _parameterCheckDelegate;
        private Func<object, object[], bool> _matchDelegate;

        internal ModelFilter(Type modelType, string name, Delegate filterDelegate)
        {
            ModelType = modelType ?? throw new ArgumentNullException(nameof(modelType));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            FilterDelegate = filterDelegate ?? throw new ArgumentNullException(nameof(filterDelegate));

            FullName = $"{modelType.Name}:{Name}";

            ParameterInfo[] requiredParameters = filterDelegate.Method.GetParameters().Skip(1).ToArray();
            BuildParameterCheckDelegate(requiredParameters);
            BuildMatchDelegate(modelType, requiredParameters);
        }

        public bool CanFilterModelType(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            return ModelType.IsAssignableFrom(type);
        }

        public bool CanFilterModelType<TModel>() where TModel : class => CanFilterModelType(typeof(TModel));

        public bool AcceptsParameters(object[] filterParams)
        {
            if (filterParams == null)
                throw new ArgumentNullException(nameof(filterParams));

            return _parameterCheckDelegate.Invoke(filterParams);
        }

        public bool Matches<TModel>(TModel entity, params object[] filterParams) where TModel : class
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));
            if (filterParams == null)
                throw new ArgumentNullException(nameof(filterParams));
            if (!CanFilterModelType(typeof(TModel)))
                throw new ArgumentException("Model type is incompatible to this model filter.", nameof(TModel));

            // The check for parameter types is skipped for performance reasons. The following call will throw exceptions anyway.
            return _matchDelegate.Invoke(entity, filterParams);
        }

        private void BuildParameterCheckDelegate(ParameterInfo[] requiredParameters)
        {
            // _parameterCheckDelegate = (object[] filterParams) => {
            //     if (filterParams.Length != 2)
            //         return false;
            //     try
            //     {
            //         Convert.ToInt32(filterParams[0]);
            //         if (!(filterParams[1] is string))
            //             return false;
            //     }
            //     catch (Exception ex)
            //     {
            //         return false;
            //     }
            //     return true;
            // };

            // Lambda parameters
            ParameterExpression lambdaFilterParamsParameter = Expression.Parameter(typeof(object[]), "filterParams");

            // Target the method returns to
            LabelTarget returnTarget = Expression.Label(typeof(bool));

            // Content of the lambda body
            var lambdaBodyExpressions = new Expression[3];

            // Add if that checks if the parameter count matches
            lambdaBodyExpressions[0] = Expression.IfThen(Expression.NotEqual(Expression.ArrayLength(lambdaFilterParamsParameter), Expression.Constant(requiredParameters.Length)),
                                                         Expression.Return(returnTarget, Expression.Constant(false)));

            // Add expressions to check the value of each parameter
            var tryBodyExpressions = new Expression[requiredParameters.Length + 1];
            LabelTarget tryBlockReturnTarget = Expression.Label(typeof(bool));
            for (var i = 0; i < requiredParameters.Length; i++)
            {
                ParameterInfo parameter = requiredParameters[i];
                Expression value = Expression.ArrayIndex(lambdaFilterParamsParameter, Expression.Constant(i));

                Expression checkExpression;
                if (ValueConverters.ContainsKey(parameter.ParameterType))
                {
                    // Use the converter to try to convert the type
                    checkExpression = ValueConverters[parameter.ParameterType].Invoke(value);
                }
                else
                {
                    // Checks if the passed parameter has the correct type
                    TypeBinaryExpression isTypeCorrectExpression =
                        Expression.TypeIs(Expression.ArrayAccess(lambdaFilterParamsParameter, Expression.Constant(i)), parameter.ParameterType);
                    checkExpression = Expression.IfThen(Expression.Not(isTypeCorrectExpression), Expression.Return(returnTarget, Expression.Constant(false)));
                }

                // Combine checks and return false if any failed
                tryBodyExpressions[i] = checkExpression;
            }

            // Execute checks in try catch block
            tryBodyExpressions[tryBodyExpressions.Length - 1] = Expression.Label(tryBlockReturnTarget, Expression.Constant(true));
            LabelTarget catchBlockReturnTarget = Expression.Label(typeof(bool));
            lambdaBodyExpressions[1] = Expression.TryCatch(Expression.Block(tryBodyExpressions),
                                                           Expression.Catch(typeof(Exception),
                                                                            Expression.Block(Expression.Return(returnTarget, Expression.Constant(false)),
                                                                                             Expression.Label(catchBlockReturnTarget, Expression.Constant(false)))));

            // Return true by default
            lambdaBodyExpressions[2] = Expression.Label(returnTarget, Expression.Constant(true));

            // Build lambdy body that returns true by default
            BlockExpression lambdaBody = Expression.Block(lambdaBodyExpressions);

            // Build and compile lambda
            _parameterCheckDelegate = Expression.Lambda<Func<object[], bool>>(lambdaBody, lambdaFilterParamsParameter).Compile();
        }

        private void BuildMatchDelegate(Type modelType, ParameterInfo[] requiredParameters)
        {
            // _matchDelegate = (object entity, object[] filterParams) => {
            //     return FilterDelegate((Model)entity, Convert.ToInt32(filterParams[0]), (string)filterParams[1]);
            // };

            // Lambda parameters
            ParameterExpression lambdaEntityParameter = Expression.Parameter(typeof(object), "entity");
            ParameterExpression lambdaFilterParamsParameter = Expression.Parameter(typeof(object[]), "filterParams");

            // Collect filter parameters
            var callParameters = new Expression[1 + requiredParameters.Length];
            callParameters[0] = Expression.Convert(lambdaEntityParameter, modelType);
            for (var i = 0; i < requiredParameters.Length; i++)
            {
                ParameterInfo parameter = requiredParameters[i];
                Expression value = Expression.ArrayIndex(lambdaFilterParamsParameter, Expression.Constant(i));

                callParameters[i + 1] = ValueConverters.ContainsKey(parameter.ParameterType)
                    ? ValueConverters[parameter.ParameterType].Invoke(value)
                    : Expression.Convert(value, parameter.ParameterType);
            }

            // Lambda body calls the filter method
            var lambdaBody = Expression.Call(null, FilterDelegate.Method, callParameters);

            // Build and compile lambda
            _matchDelegate = Expression.Lambda<Func<object, object[], bool>>(lambdaBody, lambdaEntityParameter, lambdaFilterParamsParameter).Compile();
        }
    }
}
