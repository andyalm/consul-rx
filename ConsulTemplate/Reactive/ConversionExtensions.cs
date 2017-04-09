using System.Collections.Generic;
using System.Linq;
using System.Text;
using Consul;

namespace ConsulTemplate.Reactive
{
    public static class ConversionExtensions
    {
        public static Service ToService(this CatalogService[] services)
        {
            return new Service
            {
                Id = services.First().ServiceID,
                Name = services.First().ServiceName,
                Nodes = services.Select(n => new ServiceNode
                    {
                        Address = string.IsNullOrWhiteSpace(n.ServiceAddress) ? n.Address : n.ServiceAddress,
                        Name = n.Node,
                        Port = n.ServicePort,
                        Tags = n.ServiceTags
                    })
                    .ToArray()
            };
        }

        public static KeyValuePair<string, string> ToKeyValuePair(this KVPair kv)
        {
            return new KeyValuePair<string, string>(kv.Key, Encoding.UTF8.GetString(kv.Value));
        }
    }
}