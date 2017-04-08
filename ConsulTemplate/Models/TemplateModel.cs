using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Consul;

namespace ConsulTemplateDotNet.Models
{
    public class TemplateModel
    {
        public ChangeTrackingCollection<CatalogService[]> Services { get; }
        public ChangeTrackingCollection<KVPair> KVPairs { get; }
        private object _kvMutex = new object();

        private static readonly KVPairComparer _kvComparer = new KVPairComparer();
        private static readonly CatalogServiceArrayComparer _serviceComparer = new CatalogServiceArrayComparer();

        public TemplateModel()
        {
            Services = new ChangeTrackingCollection<CatalogService[]>(_serviceComparer, s => s[0].ServiceName);
            WatchForChange(Services);
            KVPairs = new ChangeTrackingCollection<KVPair>(_kvComparer, p => p.Key);
            WatchForChange(KVPairs);
        }

        internal void UpdateService(CatalogService[] services)
        {
            Services.TryUpdate(services);
        }

        internal void UpdateKey(KVPair kv)
        {
            KVPairs.TryUpdate(kv);
        }

        private void WatchForChange<T>(ChangeTrackingCollection<T> collection)
        {
            collection.Changed += delegate
            {
                Changed?.Invoke(this, this);
            };
        }

        public event EventHandler<TemplateModel> Changed;

        private class KVPairComparer : IEqualityComparer<KVPair>
        {
            public bool Equals(KVPair x, KVPair y)
            {
                if (!x.Key.Equals(y.Key))
                    return false;

                return x.Value?.SequenceEqual(y.Value) == true;
            }

            public int GetHashCode(KVPair obj)
            {
                return obj.Key.GetHashCode() + obj.Value.GetHashCode();
            }
        }

        private class CatalogServiceArrayComparer : IEqualityComparer<CatalogService[]>
        {
            private static readonly CatalogServiceComparer _singleComparer = new CatalogServiceComparer();

            public bool Equals(CatalogService[] x, CatalogService[] y)
            {
                return x.SequenceEqual(y, _singleComparer);
            }

            public int GetHashCode(CatalogService[] obj)
            {
                return obj.Select(s => s.GetHashCode()).Sum();
            }
        }

        private class CatalogServiceComparer : IEqualityComparer<CatalogService>
        {
            public bool Equals(CatalogService x, CatalogService y)
            {
                if (x == null && y == null)
                    return true;
                if (x == null || y == null)
                    return false;

                return x?.ServiceName?.Equals(y.ServiceName) == true
                       && x.Address?.Equals(y.Address) == true
                       && x.Node?.Equals(y.Node) == true
                       && x.ServiceAddress?.Equals(y.ServiceAddress) == true
                       && x.ServiceID?.Equals(y.ServiceID) == true
                       && x.ServicePort.Equals(y.ServicePort)
                       && x.ServiceTags.SequenceEqual(y.ServiceTags);
            }

            public int GetHashCode(CatalogService obj)
            {
                throw new NotImplementedException();
            }
        }
    }

    public class ChangeTrackingCollection<T> : IEnumerable<T>
    {
        private readonly IEqualityComparer<T> _comparer;
        private readonly Func<T, string> _keyAccessor;
        private readonly ConcurrentDictionary<string, T> _dictionary = new ConcurrentDictionary<string, T>(StringComparer.OrdinalIgnoreCase);
        private readonly object _mutex = new object();

        public event EventHandler<T> Changed;

        public ChangeTrackingCollection(IEqualityComparer<T> comparer, Func<T,string> keyAccessor)
        {
            _comparer = comparer;
            _keyAccessor = keyAccessor;
        }

        public void TryUpdate(T kv)
        {
            bool changed = false;
            var key = _keyAccessor(kv);
            lock (_mutex)
            {
                if (_dictionary.TryAdd(key, kv))
                {
                    Console.WriteLine($"Adding key {key}");
                    changed = true;
                }
                else
                {
                    var existingValue = _dictionary[key];
                    if (!_comparer.Equals(kv, existingValue))
                    {
                        Console.WriteLine($"Updating key {key}");
                        _dictionary[key] = kv;
                        changed = true;
                    }
                    else
                    {
                        Console.WriteLine($"Existing value for key {key} is the same as the new value");
                    }
                }
            }

            if (changed)
            {
                Changed?.Invoke(this, kv);
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            foreach (var pair in _dictionary)
            {
                yield return pair.Value;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}