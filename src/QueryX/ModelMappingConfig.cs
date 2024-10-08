﻿using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;

namespace QueryX
{
    public class QueryMappingConfig
    {
        private static readonly ModelMapping DefaultMapping = new ModelMapping();

        private readonly ConcurrentDictionary<Type, ModelMapping> _mappings =
            new ConcurrentDictionary<Type, ModelMapping>();

        private readonly QueryConfiguration _queryConfig = Global == null ? new QueryConfiguration() : Global._queryConfig.Clone();

        public static QueryMappingConfig Global { get; } = new QueryMappingConfig();

        public QueryConfiguration QueryConfig => _queryConfig;

        public QueryMappingConfig For<TModel>(Action<ModelMappingConfig<TModel>> modelMappingConfig)
        {
            if (_mappings.TryGetValue(typeof(TModel), out var mapping))
                modelMappingConfig(new ModelMappingConfig<TModel>(mapping));

            mapping = new ModelMapping();
            _mappings.TryAdd(typeof(TModel), mapping);
            modelMappingConfig(new ModelMappingConfig<TModel>(mapping));

            return this;
        }

        public QueryMappingConfig Clear<TModel>()
        {
            if (_mappings.ContainsKey(typeof(TModel)))
                _mappings.TryRemove(typeof(TModel), out _);

            return this;
        }

        public QueryMappingConfig SetQueryConfiguration(Action<QueryConfiguration>? options = null)
        {
            options?.Invoke(_queryConfig);
            return this;
        }

        public QueryMappingConfig Clone()
        {
            var clone = new QueryMappingConfig();
            foreach (var mapping in _mappings)
                clone._mappings.TryAdd(mapping.Key, mapping.Value.Clone());

            return clone;
        }

        internal ModelMapping GetMapping<TModel>() =>
            GetMapping(typeof(TModel));

        internal ModelMapping GetMapping(Type modelType) =>
            _mappings.TryGetValue(modelType, out var mapping)
                ? mapping
                : DefaultMapping;
    }

    public class ModelMappingConfig<TModel>
    {
        private readonly ModelMapping _mapping;

        internal ModelMappingConfig(ModelMapping mapping)
        {
            _mapping = mapping;
        }

        public PropertyMappingConfig<TModel, TValue> Property<TValue>(Expression<Func<TModel, TValue>> propertyName)
        {
            if (!(propertyName.Body is MemberExpression member))
                throw new ArgumentException($"Expression '{propertyName}' refers to a method, not a property.");

            var propName = string.Join(".", member.ToString().Split('.').Skip(1));

            return new PropertyMappingConfig<TModel, TValue>(_mapping, propName);
        }
    }

    public class PropertyMappingConfig<TModel, TValue>
    {
        private readonly ModelMapping _mapping;
        private readonly string _propertyName;

        internal PropertyMappingConfig(ModelMapping mapping, string propertyName)
        {
            _mapping = mapping;
            _propertyName = propertyName;
        }

        public PropertyMappingConfig<TModel, TValue> MapFrom(string source)
        {
            _mapping.AddPropertyMapping(_propertyName, source);
            return this;
        }

        public PropertyMappingConfig<TModel, TValue> MapFrom(string source, Func<string, TValue> convert)
        {
            _mapping.AddPropertyMapping(_propertyName, source, convert);
            return this;
        }

        public PropertyMappingConfig<TModel, TValue> CustomFilter(
            Func<ParameterExpression, TValue[], FilterOperator, Expression> filter)
        {
            _mapping.AddCustomFilter<TModel, TValue>(_propertyName, filter);
            return this;
        }

        public PropertyMappingConfig<TModel, TValue> CustomSort(
            Func<IOrderedQueryable<TModel>, bool, bool, IQueryable<TModel>> sort)
        {
            _mapping.AddCustomSort(_propertyName, sort);
            return this;
        }

        public PropertyMappingConfig<TModel, TValue> IgnoreFilter()
        {
            _mapping.IgnoreFilter(_propertyName);
            return this;
        }

        public PropertyMappingConfig<TModel, TValue> IgnoreSort()
        {
            _mapping.IgnoreSort(_propertyName);
            return this;
        }

        public void Ignore()
        {
            _mapping.Ignore(_propertyName);
        }
    }
}