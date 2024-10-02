using System.Linq;
using System;
using System.Linq.Expressions;
using QueryX.Utils;

namespace QueryX
{
    internal interface ICustomFilter
    {
    }

    internal interface ICustomFilter<TModel> : ICustomFilter
    {
        Expression Apply(ParameterExpression parameters, string?[] values, FilterOperator @operator);
    }

    internal class CustomFilter<TModel, TValue> : ICustomFilter<TModel>
    {
        private readonly Func<ParameterExpression, TValue[], FilterOperator, Expression>
            _customFilterDelegate;

        internal CustomFilter(
            Func<ParameterExpression, TValue[], FilterOperator, Expression>
                customFilterDeleagate)
        {
            _customFilterDelegate = customFilterDeleagate;
        }

        Expression ICustomFilter<TModel>.Apply(ParameterExpression parameters, string?[] values,
            FilterOperator @operator)
        {
            var typedValues = values.Select(v => v.ConvertValue(typeof(TValue))).Cast<TValue>().ToArray();
            return _customFilterDelegate(parameters, typedValues, @operator);
        }
    }
}