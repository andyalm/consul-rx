using System;
using System.Collections.Generic;
using System.Reactive.Linq;

namespace ConsulTemplate.Reactive
{
    public class TemplateModel
    {
        public ChangeTrackingCollection<Service> Services { get; }
        public ChangeTrackingCollection<KeyValuePair<string,string>> KVPairs { get; }
        public IObservable<TemplateModel> Changes { get; }

        public TemplateModel()
        {
            Services = new ChangeTrackingCollection<Service>(s => s.Name);
            KVPairs = new ChangeTrackingCollection<KeyValuePair<string,string>>( p => p.Key);

            Changes = Services.Changes.Select(_ => this).Merge(KVPairs.Changes.Select(_ => this));
        }

        internal void UpdateService(Service service)
        {
            Services.TryUpdate(service);
        }

        internal void UpdateKey(KeyValuePair<string,string> kv)
        {
            KVPairs.TryUpdate(kv);
        }
    }
}