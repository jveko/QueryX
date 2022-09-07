﻿using System.Linq.Expressions;

namespace QueryX.Filters
{
    public class StartsWithFilter : IFilter
    {
        public StartsWithFilter(string value)
        {
            Value = value;
        }

        public OperatorType Operator => OperatorType.StartsWith;
        public string Value { get; set; }

        public Expression GetExpression(Expression property)
        {
            return Expression.Call(property, Methods.StartsWith, Expression.Constant(Value, typeof(string)));
        }
    }
}
