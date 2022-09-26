﻿using System.Collections.Generic;
using System.Linq;

namespace QueryX.Parser.Nodes
{
    public class OperatorNode : NodeBase
    {
        public OperatorNode(string property, string @operator, IEnumerable<string?> values, bool isNegated = false)
        {
            Property = property;
            Operator = @operator;
            Values = values.ToList();
            IsNegated = isNegated;
        }

        public string Property { get; set; }
        public string Operator { get; set; }
        public IEnumerable<string?> Values { get; set; }

        public override void Accept(INodeVisitor visitor) => visitor.Visit(this);

        public override NodeBase Negated()
        {
            return new OperatorNode(Property, Operator, Values, !IsNegated);
        }
    }
}
