using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace ConsulRx.Configuration
{
    public abstract class ServiceConfigMapping
    {
        protected ServiceConfigMapping(string configKey, string serviceName)
        {
            ConfigKey = configKey;
            ServiceName = serviceName;
        }
        
        public string ConfigKey { get; }
        public string ServiceName { get; }

        public abstract void BindToConfiguration(Service service, Dictionary<string, string> config);
    }

    public class SingleNodeServiceConfigMapping : ServiceConfigMapping
    {
        public SingleNodeServiceConfigMapping(string configKey, string serviceName, Func<ServiceNode, string> endpointFormatter, Func<ServiceNode[], ServiceNode> nodeSelector) : base(configKey, serviceName)
        {
            EndpointFormatter = endpointFormatter;
            NodeSelector = nodeSelector;
        }

        private Func<ServiceNode, string> EndpointFormatter { get; }
        private Func<ServiceNode[], ServiceNode> NodeSelector { get; }

        public override void BindToConfiguration(Service service, Dictionary<string, string> config)
        {
            try
            {
                var selectedNode = NodeSelector(service.Nodes);

                config[ConfigKey] = EndpointFormatter(selectedNode);
            }
            catch (NodeSelectionException ex)
            {
                throw new ConsulRxConfigurationException($"An error occurred when selecting a node for the consul " +
                                                         $"service {service.Name}: {ex.Message}", ex);
            }
        }
    }

    public class MultipleNodeServiceConfigMapping : ServiceConfigMapping
    {
        public MultipleNodeServiceConfigMapping(string configKey, string serviceName,
            Func<ServiceNode,string> endpointFormatter,
            Func<ServiceNode[], IEnumerable<ServiceNode>> nodeSelector = null) : base(configKey, serviceName)
        {
            EndpointFormatter = endpointFormatter ?? throw new ArgumentNullException(nameof(endpointFormatter));
            NodeSelector = nodeSelector ?? (nodes => nodes);
        }

        private Func<ServiceNode[], IEnumerable<ServiceNode>> NodeSelector { get; }
        private Func<ServiceNode, string> EndpointFormatter { get; }

        public override void BindToConfiguration(Service service, Dictionary<string, string> config)
        {
            try
            {
                var selectedNodes = NodeSelector(service.Nodes).ToArray();
                for (int i = 0; i < selectedNodes.Length; i++)
                {
                    config[$"{ConfigKey}:{i}"] = EndpointFormatter(selectedNodes[i]);
                }
            }
            catch (NodeSelectionException ex)
            {
                throw new ConsulRxConfigurationException($"An error occurred when selecting a node for the consul " +
                                                         $"service {service.Name}: {ex.Message}", ex);
            }
        }
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