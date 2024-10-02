using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace QueryX
{
    internal class ModelMapping
    {
        private readonly Dictionary<string, (string TargetProperty, dynamic? Convert)> _propertyMapping =
            new Dictionary<string, (string, dynamic?)>(StringComparer.OrdinalIgnoreCase);

        private readonly HashSet<string> _ignoredFilter = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _ignoredSort = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, ICustomFilter> _customFilters =
            new Dictionary<string, ICustomFilter>(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, dynamic> _customSorts =
            new Dictionary<string, dynamic>(StringComparer.OrdinalIgnoreCase);

        internal void AddPropertyMapping(string targetProperty, string sourceName)
        {
            _propertyMapping[sourceName] = (targetProperty, null);
        }

        internal void AddPropertyMapping<TFrom, TValue>(string targetProperty, string sourceName,
            Func<TFrom, TValue> convert)
        {
            _propertyMapping[sourceName] = (targetProperty, convert);
        }

        internal (string TargetProperty, dynamic? Convert) GetPropertyMapping(string sourceName)
        {
            return _propertyMapping.TryGetValue(sourceName, out var propMapping)
                ? propMapping
                : (sourceName, null);
        }

        internal void IgnoreFilter(string propertyName)
        {
            _ignoredFilter.Add(propertyName);
        }

        internal bool FilterIsIgnored(string propertyName)
        {
            return _ignoredFilter.Contains(propertyName);
        }

        internal void IgnoreSort(string propertyName)
        {
            _ignoredSort.Add(propertyName);
        }

        internal bool SortIsIgnored(string propertyName)
        {
            return _ignoredSort.Contains(propertyName);
        }

        internal void Ignore(string propertyName)
        {
            _ignoredFilter.Add(propertyName);
            _ignoredSort.Add(propertyName);
        }

        internal void AddCustomFilter<TModel, TValue>(string propertyName,
            Func<ParameterExpression, TValue[], FilterOperator, Expression> customFilterDelegate)
        {
            var customFilter = new CustomFilter<TModel, TValue>(customFilterDelegate);
            _customFilters[propertyName] = customFilter;
        }

        internal bool HasCustomFilter(string propertyName)
        {
            return _customFilters.ContainsKey(propertyName);
        }

        internal Expression? ApplyCustomFilters<TModel>(ParameterExpression parameters, string propertyName,
            string?[] values, FilterOperator @operator)
        {
            if (!_customFilters.TryGetValue(propertyName, out var customFilter))
                return null;

            if (!(customFilter is ICustomFilter<TModel> typedCustomFilter))
                return null;

            return typedCustomFilter.Apply(parameters, values, @operator);
        }


        internal void AddCustomSort<TModel>(string propertyName,
            Func<IOrderedQueryable<TModel>, bool, bool, IQueryable<TModel>> sortDelegate)
        {
            _customSorts[propertyName] = sortDelegate;
        }

        internal bool HasCustomSort(string propertyName)
        {
            return _customSorts.ContainsKey(propertyName);
        }

        internal IQueryable<TModel> ApplyCustomSort<TModel>(string propertyName, IOrderedQueryable<TModel> source,
            bool ascending, bool isOrdered)
        {
            return !_customSorts.TryGetValue(propertyName, out var sortDelegate)
                ? source
                : (IQueryable<TModel>)sortDelegate(source, ascending, isOrdered);
        }

        internal ModelMapping Clone()
        {
            var clone = new ModelMapping();
            foreach (var mapping in _propertyMapping)
                clone._propertyMapping.Add(mapping.Key, mapping.Value);

            foreach (var ignoredProperty in _ignoredSort)
                clone._ignoredSort.Add(ignoredProperty);

            return clone;
        }
    }
}