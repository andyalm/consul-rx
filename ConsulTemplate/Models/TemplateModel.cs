using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Consul;

namespace ConsulTemplateDotNet.Models
{
    public class TemplateModel
    {
        public ConcurrentDictionary<string, CatalogService[]> Services { get; }
        public ConcurrentDictionary<string, KVPair> KVPairs { get; }

        public TemplateModel()
        {
            Services = new ConcurrentDictionary<string, CatalogService[]>();
            KVPairs = new ConcurrentDictionary<string, KVPair>();
        }

        public void UpdateService(CatalogService[] services)
        {
            Console.WriteLine($"Updating service {services[0].ServiceName}");
            Services.AddOrUpdate(services[0].ServiceName, _ => services, (x, y) => services);
        }

        internal void UpdateKey(KVPair kv)
        {
            Console.WriteLine($"Updating key {kv.Key}");
            KVPairs.AddOrUpdate(kv.Key, _ => kv, (x, y) => kv);
        }
    }
}