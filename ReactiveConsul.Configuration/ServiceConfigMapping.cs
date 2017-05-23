using System;
using System.Collections.ObjectModel;

namespace ReactiveConsul.Configuration
{
    public class ServiceConfigMapping
    {
        public ServiceConfigMapping(string configKey, string serviceName, IEndpointBuilder endpointBuilder)
        {
            ConfigKey = configKey;
            ServiceName = serviceName;
            EndpointBuilder = endpointBuilder;
        }
        
        public string ConfigKey { get; }
        public string ServiceName { get; }
        public IEndpointBuilder EndpointBuilder { get; }
    }

    public class ServiceConfigMappingCollection : KeyedCollection<string, ServiceConfigMapping>
    {
        public ServiceConfigMappingCollection() : base(StringComparer.OrdinalIgnoreCase) {}
        
        protected override string GetKeyForItem(ServiceConfigMapping item)
        {
            return item.ConfigKey;
        }
    }
}