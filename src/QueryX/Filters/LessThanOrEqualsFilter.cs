﻿using System.Linq.Expressions;

namespace QueryX.Filters
{
    public class LessThanOrEqualsFilter<TValue> : IFilter
    {
        public LessThanOrEqualsFilter(TValue value)
        {
            Value = value;
        }

        public string Operator => OperatorType.LessThanOrEqualsFilter;
        public TValue Value { get; set; }

        public Expression GetExpression(Expression property)
        {
            return Expression.LessThanOrEqual(property, Expression.Constant(Value, typeof(TValue)));
        }
    }
}
