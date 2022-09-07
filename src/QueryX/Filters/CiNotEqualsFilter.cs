﻿using System.Linq.Expressions;

namespace QueryX.Filters
{
    public class CiNotEqualsFilter : IFilter
    {
        public CiNotEqualsFilter(string value)
        {
            Value = value;
        }

        public OperatorType Operator => OperatorType.CiNotEquals;
        public string Value { get; set; }

        public Expression GetExpression(Expression property)
        {
            Expression toLowerExp = Expression.Call(property, Methods.ToLower);

            return Expression.NotEqual(toLowerExp, Expression.Constant(Value?.ToLower(), typeof(string)));
        }
    }
}
