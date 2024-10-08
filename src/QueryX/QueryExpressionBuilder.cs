﻿using QueryX.Exceptions;
using QueryX.Parsing.Nodes;
using QueryX.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace QueryX
{
    internal class QueryExpressionBuilder<TModel> : INodeVisitor
    {
        private static MethodInfo AnyMethod => typeof(Enumerable).GetMethods()
            .First(m => m.Name == "Any" && m.GetParameters().Count() == 2);

        private static MethodInfo AllMethod => typeof(Enumerable).GetMethods()
            .First(m => m.Name == "All" && m.GetParameters().Count() == 2);

        private readonly string? _filter;
        private readonly Stack<Context> _contexts;
        private readonly QueryMappingConfig _mappingConfig;

        private readonly Dictionary<string, (string?[] Values, FilterOperator Operator)> _customFilters =
            new Dictionary<string, (string?[], FilterOperator)>(StringComparer.OrdinalIgnoreCase);

        public QueryExpressionBuilder(string? filter, QueryMappingConfig mappingConfig)
        {
            _filter = filter;
            _contexts = new Stack<Context>();
            _mappingConfig = mappingConfig;
            _contexts.Push(new Context(typeof(TModel), string.Empty, Expression.Parameter(typeof(TModel), "m")));
            if (_filter == null || string.IsNullOrEmpty(_filter)) return;
            var nodes = Parsing.QueryParser.ParseNodes(_filter);
            Visit(nodes as dynamic);
        }

        public void Visit(OrElseNode node)
        {
            var context = _contexts.First();

            node.Left.Accept(this);
            node.Right.Accept(this);

            var right = context.Stack.Pop();
            var left = context.Stack.Pop();

            var exp = left switch
            {
                null when right == null => null,
                null => node.IsNegated ? Expression.Not(right) : right,
                _ when right == null => node.IsNegated ? Expression.Not(left) : left,
                _ => node.IsNegated
                    ? (Expression) Expression.Not(Expression.OrElse(left, right))
                    : Expression.OrElse(left, right)
            };

            context.Stack.Push(exp);
        }

        public void Visit(AndAlsoNode node)
        {
            var context = _contexts.First();

            node.Left.Accept(this);
            node.Right.Accept(this);

            var right = context.Stack.Pop();
            var left = context.Stack.Pop();

            var exp = left switch
            {
                null when right == null => null,
                null => node.IsNegated ? Expression.Not(right) : right,
                _ when right == null => node.IsNegated ? Expression.Not(left) : left,
                _ => node.IsNegated
                    ? (Expression) Expression.Not(Expression.AndAlso(left, right))
                    : Expression.AndAlso(left, right)
            };

            context.Stack.Push(exp);
        }

        public void Visit(FilterNode node)
        {
            var context = _contexts.First();

            if (!node.Property.TryResolvePropertyName(context.ParentType, _mappingConfig, out var resolvedName))
            {
                if (_mappingConfig.QueryConfig.ThrowingQueryExceptions)
                    throw new InvalidFilterPropertyException(node.Property);

                context.Stack.Push(null);
                return;
            }

            var modelMapping = _mappingConfig.GetMapping(context.ParentType);

            if (modelMapping.FilterIsIgnored(resolvedName))
            {
                context.Stack.Push(null);
                return;
            }

            if (modelMapping.HasCustomFilter(resolvedName))
            {
                var mapping = _mappingConfig.GetMapping(typeof(TModel));
                var expression =
                    mapping.ApplyCustomFilters<TModel>(context.Parameter, resolvedName, node.Values, node.Operator);
                context.Stack.Push(expression);
                return;
            }

            var propExp = resolvedName.GetPropertyExpression(context.Parameter) ??
                          throw new InvalidFilterPropertyException(node.Property);

            context.Stack.Push(node.GetExpression(propExp, modelMapping));
        }

        public void Visit(CollectionFilterNode node)
        {
            var context = _contexts.First();

            if (!node.Property.TryResolvePropertyName(context.ParentType, _mappingConfig, out var resolvedName))
            {
                if (_mappingConfig.QueryConfig.ThrowingQueryExceptions)
                    throw new InvalidFilterPropertyException(node.Property);

                context.Stack.Push(null);
                return;
            }

            if (_mappingConfig.GetMapping(context.ParentType).FilterIsIgnored(resolvedName))
            {
                context.Stack.Push(null);
                return;
            }

            var propertyInfo = resolvedName.GetPropertyInfo<TModel>()
                               ?? throw new InvalidFilterPropertyException(resolvedName);
            var genericTargetTypee = propertyInfo.PropertyType.GetGenericArguments();
            if (!propertyInfo.PropertyType.GetGenericArguments().Any())
                throw new InvalidFilterPropertyException(resolvedName);

            var genericTargetType = propertyInfo.PropertyType.GetGenericArguments()[0];

            var modelParameter = Expression.Parameter(genericTargetType, "s");
            var subContext = new Context(genericTargetType, context.PropertyName, modelParameter);
            _contexts.Push(subContext);

            Visit(node.Filter as dynamic);

            var lastExp = subContext.Stack.Last();
            if (lastExp == null)
            {
                context.Stack.Push(null);
                _contexts.Pop();
                return;
            }

            var exp = Expression.Lambda(lastExp, modelParameter);

            var method = node.ApplyAll ? AllMethod : AnyMethod;
            var methodGeneric = method.MakeGenericMethod(genericTargetType);

            var propExp = resolvedName.GetPropertyExpression(context.Parameter)
                          ?? throw new InvalidFilterPropertyException(resolvedName);
            Expression anyExp = Expression.Call(null, methodGeneric, propExp, exp);

            if (node.IsNegated)
                anyExp = Expression.Not(anyExp);

            context.Stack.Push(anyExp);
            _contexts.Pop();
        }

        public Expression<Func<TModel, bool>>? GetFilterExpression()
        {
            var context = _contexts.First();

            if (context.Stack.Count == 0)
                return null;

            var exp = context.Stack.Pop();
            return exp == null ? null : Expression.Lambda<Func<TModel, bool>>(exp!, context.Parameter);
        }

        public IQueryable<TModel> ApplyCustomFilters(IQueryable<TModel> source)
        {
            return source;
        }

        private class Context
        {
            public Context(Type parentType, string propertyName, ParameterExpression parameter)
            {
                ParentType = parentType;
                PropertyName = propertyName;
                Parameter = parameter;
                Stack = new Stack<Expression?>();
            }

            public Type ParentType { get; }
            public string PropertyName { get; }
            public ParameterExpression Parameter { get; }
            public Stack<Expression?> Stack { get; }

            public string GetConcatenatedProperty(string propertyName)
            {
                return string.IsNullOrEmpty(PropertyName)
                    ? propertyName
                    : $"{PropertyName}.{propertyName}";
            }
        }
    }
}