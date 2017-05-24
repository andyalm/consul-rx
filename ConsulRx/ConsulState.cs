using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace ConsulRx
{
    public class ConsulState
    {
        public ChangeTrackingCollection<Service> Services { get; }
        public KeyValueStore KVStore { get; }
        public ChangeTrackingCollection<string> MissingKeyPrefixes { get; }
        public IObservable<ConsulState> Changes { get; }

        public ConsulState()
        {
            Services = new ChangeTrackingCollection<Service>(s => s.Name);
            KVStore = new KeyValueStore();
            MissingKeyPrefixes = new ChangeTrackingCollection<string>(v => v);

            Changes = Services.Changes.Select(_ => this).Merge(KVStore.Changes.Select(_ => this)).Merge(MissingKeyPrefixes.Changes.Select(_ => this));
        }

        public void UpdateService(Service service)
        {
            Services.TryUpdate(service);
        }

        public void UpdateKVNode(KeyValueNode kvNode)
        {
            KVStore.Update(kvNode);
        }

        public void UpdateKVNodes(IEnumerable<KeyValueNode> kvNodes)
        {
            KVStore.Update(kvNodes);
        }

        public void MarkKeyPrefixAsMissingOrEmpty(string keyPrefix)
        {
            MissingKeyPrefixes.TryUpdate(keyPrefix);
        }

        public bool SatisfiesAll(ConsulDependencies consulDependencies)
        {
            return consulDependencies.Services.IsSubsetOf(Services.Select(s => s.Name))
                   && consulDependencies.Keys.IsSubsetOf(KVStore.Select(s => s.FullKey))
                   && consulDependencies.KeyPrefixes.All(p => KVStore.Any(k => k.FullKey.StartsWith(p)) || MissingKeyPrefixes.Contains(p));
        }
    }
}