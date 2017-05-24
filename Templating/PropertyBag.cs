using System;
using System.Collections.Generic;
using System.Reflection;

namespace ConsulRx.Templating
{
    public class PropertyBag
    {
        private readonly IDictionary<string, object> _properties = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        public PropertyBag(object obj = null)
        {
            if (obj != null)
            {
                ReflectObject(obj);
            }
        }

        public PropertyBag(IDictionary<string, object> properties)
        {
            _properties = properties;
        }

        public T Value<T>(string name)
        {
            if (_properties.TryGetValue(name, out var value))
            {
                return (T) value;
            }
            else
            {
                return default(T);
            }
        }

        private void ReflectObject(object obj)
        {
            foreach (var property in obj.GetType().GetTypeInfo().GetProperties())
            {
                _properties[property.Name] = property.GetValue(obj);
            }
        }
    }
}