using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Collections.Immutable;

namespace ConsulRx
{
    public class ConsulState
    {
        private ImmutableDictionary<string, Service> _services;
        public IEnumerable<Service> Services => _services.Values;
        
        public KeyValueStore KVStore { get; }

        public IEnumerable<string> MissingKeyPrefixes => _missingKeyPrefixes;
        private ImmutableHashSet<string> _missingKeyPrefixes;

        public ConsulState()
        {
            _services = ImmutableDictionary<string,Service>.Empty.WithComparers(StringComparer.OrdinalIgnoreCase);
            KVStore = new KeyValueStore();
            _missingKeyPrefixes = ImmutableHashSet<string>.Empty.WithComparer(StringComparer.OrdinalIgnoreCase);
        }

        public ConsulState(ImmutableDictionary<string, Service> services, KeyValueStore kvStore,
            ImmutableHashSet<string> missingKeyPrefixes)
        {
            _services = services;
            KVStore = kvStore;
            _missingKeyPrefixes = missingKeyPrefixes;
        }

        public ConsulState UpdateService(Service service)
        {
            return new ConsulState(_services.SetItem(service.Name, service), KVStore, _missingKeyPrefixes);
        }

        public ConsulState UpdateKVNode(KeyValueNode kvNode)
        {
            return new ConsulState(_services, KVStore.Update(kvNode), _missingKeyPrefixes);
            
        }

        public ConsulState UpdateKVNodes(IEnumerable<KeyValueNode> kvNodes)
        {
            return new ConsulState(_services, KVStore.Update(kvNodes), _missingKeyPrefixes);
        }

        public ConsulState MarkKeyPrefixAsMissingOrEmpty(string keyPrefix)
        {
            return new ConsulState(_services, KVStore, _missingKeyPrefixes.Add(keyPrefix));
        }

        public bool SatisfiesAll(ConsulDependencies consulDependencies)
        {
            return consulDependencies.Services.IsSubsetOf(_services.Keys)
                   && consulDependencies.Keys.IsSubsetOf(KVStore.Select(s => s.FullKey))
                   && consulDependencies.KeyPrefixes.All(p => KVStore.Any(k => k.FullKey.StartsWith(p)) || _missingKeyPrefixes.Contains(p));
        }

        public Service GetService(string serviceName)
        {
            if (_services.TryGetValue(serviceName, out var service))
                return service;

            return null;
        }
    }
}