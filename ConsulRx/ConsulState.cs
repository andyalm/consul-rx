using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Immutable;

namespace ConsulRx
{
    public class ConsulState
    {
        private readonly ImmutableDictionary<string, Service> _services;
        public IEnumerable<Service> Services => _services.Values;
        
        public KeyValueStore KVStore { get; }

        public IEnumerable<string> MissingKeyPrefixes => _missingKeyPrefixes;
        private readonly ImmutableHashSet<string> _missingKeyPrefixes;

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
            if (TryUpdateService(service, out var updatedState))
                return updatedState;

            return this;
        }

        public bool TryUpdateService(Service service, out ConsulState updatedState)
        {
            if (_services.TryGetValue(service.Name, out var existingService) && existingService.Equals(service))
            {
                updatedState = null;
                return false;
            }

            updatedState = new ConsulState(_services.SetItem(service.Name, service), KVStore, _missingKeyPrefixes);
            return true;
        }

        public ConsulState UpdateKVNode(KeyValueNode kvNode)
        {
            if (TryUpdateKVNode(kvNode, out var updatedState))
                return updatedState;

            return this;
        }

        public bool TryUpdateKVNode(KeyValueNode kvNode, out ConsulState updatedState)
        {
            if (KVStore.TryUpdate(kvNode, out var updatedKvStore))
            {
                updatedState = new ConsulState(_services, updatedKvStore, _missingKeyPrefixes);
                return true;
            }
            updatedState = null;
            return false;
        }

        public ConsulState UpdateKVNodes(IEnumerable<KeyValueNode> kvNodes)
        {
            if (TryUpdateKVNodes(kvNodes, out var updatedState))
                return updatedState;

            return this;
        }

        public bool TryUpdateKVNodes(IEnumerable<KeyValueNode> kvNodes, out ConsulState updatedState)
        {
            if (KVStore.TryUpdate(kvNodes, out var updatedKvStore))
            {
                updatedState = new ConsulState(_services, updatedKvStore, _missingKeyPrefixes);
                return true;
            }
            updatedState = null;
            return false;
        }

        public bool TryMarkKeyPrefixAsMissingOrEmpty(string keyPrefix, out ConsulState updatedState)
        {
            if (_missingKeyPrefixes.Contains(keyPrefix))
            {
                updatedState = null;
                return false;
            }
            updatedState = new ConsulState(_services, KVStore, _missingKeyPrefixes.Add(keyPrefix));
            return true;
        }

        public bool SatisfiesAll(ConsulDependencies consulDependencies)
        {
            return consulDependencies.Services.IsSubsetOf(_services.Keys)
                   && consulDependencies.Keys.IsSubsetOf(KVStore.Select(s => s.FullKey))
                   && consulDependencies.KeyPrefixes.All(p => KVStore.Any(k => k.FullKey.StartsWith(p)) || _missingKeyPrefixes.Contains(p));
        }

        public bool ContainsKey(string key)
        {
            return KVStore.ContainsKey(key);
        }

        public bool ContainsService(string serviceName)
        {
            return _services.ContainsKey(serviceName);
        }

        public Service GetService(string serviceName)
        {
            if (_services.TryGetValue(serviceName, out var service))
                return service;

            return null;
        }

        public bool ContainsKeyStartingWith(string keyPrefix)
        {
            return KVStore.ContainsKeyStartingWith(keyPrefix);
        }
    }
}