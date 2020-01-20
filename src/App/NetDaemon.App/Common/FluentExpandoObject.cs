﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace JoySoftware.HomeAssistant.NetDaemon.Common
{
    /// <summary>
    ///     A custom expando object to alow to return null values if properties does not exist
    /// </summary>
    /// <remarks>
    ///     Thanks to @lukevendediger for original code and inspiration
    ///     https://gist.github.com/lukevenediger/6327599
    /// </remarks>
    public class FluentExpandoObject : DynamicObject, IDictionary<string, object>
    {
        private readonly Dictionary<string, object> _dict = new Dictionary<string, object>();
        private readonly bool _ignoreCase;
        private readonly bool _returnNullMissingProperties;

        /// <summary>
        ///     Creates a BetterExpando object/
        /// </summary>
        /// <param name="ignoreCase">Don't be strict about property name casing.</param>
        /// <param name="returnNullMissingProperties">If true, returns String.Empty for missing properties.</param>
        /// <param name="root">An ExpandoObject to consume and expose.</param>
        public FluentExpandoObject(bool ignoreCase = false,
            bool returnNullMissingProperties = false,
            ExpandoObject? root = null)
        {
            _ignoreCase = ignoreCase;
            _returnNullMissingProperties = returnNullMissingProperties;
            if (root != null) Augment(root);
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return _dict.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable) _dict).GetEnumerator();
        }

        public void Add(KeyValuePair<string, object> item)
        {
            _dict.Add(item.Key, item.Value);
        }

        public void Clear()
        {
            _dict.Clear();
        }

        public bool Contains(KeyValuePair<string, object> item)
        {
            return _dict.Contains(item);
        }

        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool Remove(KeyValuePair<string, object> item)
        {
            return _dict.Remove(item.Key, out _);
        }

        public int Count => _dict.Count;

        public bool IsReadOnly => false;

        public void Add(string key, object value)
        {
            _dict.Add(key, value);
        }

        public bool ContainsKey(string key)
        {
            return _dict.ContainsKey(key);
        }

        public bool Remove(string key)
        {
            return _dict.Remove(key);
        }

        public bool TryGetValue(string key, out object value)
        {
#pragma warning disable CS8601 // Possible null reference assignment.
            return _dict.TryGetValue(key, out value);
#pragma warning restore CS8601 // Possible null reference assignment.
        }

        public object this[string key]
        {
            get => _dict[key];
            set => _dict[key] = value;
        }

        public ICollection<string> Keys => ((IDictionary<string, object>) _dict).Keys;

        public ICollection<object> Values => ((IDictionary<string, object>) _dict).Values;

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            UpdateDictionary(binder.Name, value);
            return true;
        }

        public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object value)
        {
            if (!(indexes[0] is string)) return base.TrySetIndex(binder, indexes, value);

            if (indexes[0] is string key) UpdateDictionary(NormalizePropertyName(key), value);
            return true;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            var key = NormalizePropertyName(binder.Name);

            if (_dict.ContainsKey(key))
            {
                result = _dict[key];
                return true;
            }

            if (!_returnNullMissingProperties) return base.TryGetMember(binder, out result);

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            result = null;
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
            return true;
        }

        /// <summary>
        ///     Combine two instances together to get a union.
        /// </summary>
        /// <returns>This instance but with additional properties</returns>
        /// <remarks>Existing properties are not overwritten.</remarks>
        public dynamic Augment(FluentExpandoObject obj)
        {
            obj._dict
                .Where(pair => !_dict.ContainsKey(NormalizePropertyName(pair.Key)))
                .ToList()
                .ForEach(pair => UpdateDictionary(pair.Key, pair.Value));
            return this;
        }

        public dynamic Augment(ExpandoObject obj)
        {
            obj
                .Where(pair => !_dict.ContainsKey(NormalizePropertyName(pair.Key)))
                .ToList()
                .ForEach(pair => UpdateDictionary(pair.Key, pair.Value));
            return this;
        }

        public T ValueOrDefault<T>(string propertyName, T defaultValue)
        {
            propertyName = NormalizePropertyName(propertyName);
            return _dict.ContainsKey(propertyName)
                ? (T) _dict[propertyName]
                : defaultValue;
        }

        /// <summary>
        ///     Check if BetterExpando contains a property.
        /// </summary>
        /// <remarks>Respects the case sensitivity setting</remarks>
        public bool HasProperty(string name)
        {
            return _dict.ContainsKey(NormalizePropertyName(name));
        }

        /// <summary>
        ///     Returns this object as comma-separated name-value pairs.
        /// </summary>
        public override string ToString()
        {
            return string.Join(", ", _dict.Select(pair => pair.Key + " = " + pair.Value ?? "(null)").ToArray());
        }

        private void UpdateDictionary(string name, object value)
        {
            var key = NormalizePropertyName(name);
            if (_dict.ContainsKey(key))
                _dict[key] = value;
            else
                _dict.Add(key, value);
        }

        private string NormalizePropertyName(string propertyName)
        {
            return _ignoreCase ? propertyName.ToLower() : propertyName;
        }
    }
}