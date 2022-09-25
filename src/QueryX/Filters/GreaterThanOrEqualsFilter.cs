﻿using QueryX.Utils;
using System.Linq.Expressions;

namespace QueryX.Filters
{
    public class GreaterThanOrEqualsFilter<TValue> : IFilter
    {
        public GreaterThanOrEqualsFilter(TValue value, bool isNegated)
        {
            Value = value;
            IsNegated = isNegated;
        }

        public OperatorType Operator => OperatorType.GreaterThanOrEquals;
        public TValue Value { get; }
        public bool IsNegated { get; }

        public Expression GetExpression(Expression property)
        {
            var exp = Expression.GreaterThanOrEqual(property, Value.CreateConstantFor(property));

            if(IsNegated)
                return Expression.Not(exp);

            return exp;
        }
    }
}
