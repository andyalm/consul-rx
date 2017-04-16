using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using ConsulTemplate.Templating;

namespace ConsulTemplate.Reactive
{
    public class ConsulState
    {
        public ChangeTrackingCollection<Service> Services { get; }
        public KeyValueStore KVStore { get; }
        public IObservable<ConsulState> Changes { get; }

        public ConsulState()
        {
            Services = new ChangeTrackingCollection<Service>(s => s.Name);
            KVStore = new KeyValueStore();

            Changes = Services.Changes.Select(_ => this).Merge(KVStore.Changes.Select(_ => this));
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

        public bool SatisfiesAll(TemplateDependencies templateDependencies)
        {
            return templateDependencies.Services.IsSubsetOf(Services.Select(s => s.Name))
                   && templateDependencies.Keys.IsSubsetOf(KVStore.Select(s => s.FullKey));
        }
    }
}