using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace ConsulRx.Configuration
{
    public class ConsulConfigurationSource : IConfigurationSource
    {
        private readonly ObservableConsulConfiguration _consulConfig = new ObservableConsulConfiguration();
        private readonly ConsulDependencies _consulDependencies = new ConsulDependencies();
        private readonly ServiceConfigMappingCollection _serviceConfigMappings = new ServiceConfigMappingCollection();
        private readonly KVTreeConfigMappingCollection _kvTreeConfigMappings = new KVTreeConfigMappingCollection();
        private readonly KVItemConfigMappingCollection _kvItemConfigMappings = new KVItemConfigMappingCollection();
        private IEmergencyCache _cache = new FileSystemEmergencyCache();
        private TimeSpan? _retryDelay = null;

        public ConsulConfigurationSource Endpoint(string consulEndpoint)
        {
            _consulConfig.Endpoint = consulEndpoint;

            return this;
        }

        public ConsulConfigurationSource MapService(string consulServiceName, string configKey,
            Func<ServiceNode, string> endpointFormatter, Func<ServiceNode[], ServiceNode> nodeSelector)
        {
            _consulDependencies.Services.Add(consulServiceName);
            _serviceConfigMappings.Add(new SingleNodeServiceConfigMapping(configKey, consulServiceName, endpointFormatter, nodeSelector));
            
            return this;
        }
        
        public ConsulConfigurationSource MapService(string consulServiceName, string configKey,
            Func<ServiceNode, string> endpointFormatter, Func<ServiceNode[], IEnumerable<ServiceNode>> nodeSelector)
        {
            _consulDependencies.Services.Add(consulServiceName);
            _serviceConfigMappings.Add(new MultipleNodeServiceConfigMapping(configKey, consulServiceName, endpointFormatter, nodeSelector));
            
            return this;
        }

        public ConsulConfigurationSource MapHttpService(string consulServiceName, string configKey)
        {
            return MapService(consulServiceName, configKey, EndpointFormatters.Http, NodeSelectors.First);
        }
        
        public ConsulConfigurationSource MapHttpsService(string consulServiceName, string configKey)
        {
            return MapService(consulServiceName, configKey, EndpointFormatters.Https, NodeSelectors.First);
        }

        public ConsulConfigurationSource MapKeyPrefix(string consulKeyPrefix, string configKey)
        {
            _consulDependencies.KeyPrefixes.Add(consulKeyPrefix);
            _kvTreeConfigMappings.Add(new KVTreeConfigMapping(configKey, consulKeyPrefix));

            return this;
        }

        public ConsulConfigurationSource MapKey(string consulKey, string configKey)
        {
            _consulDependencies.Keys.Add(consulKey);
            _kvItemConfigMappings.Add(new KVItemConfigMapping(configKey, consulKey));

            return this;
        }

        /// <summary>
        /// Configures an automatic update every 15 seconds
        /// </summary>
        /// <returns>
        /// The same instance on which the method was called.
        /// </returns>
        public ConsulConfigurationSource AutoUpdate() =>
            AutoUpdate(TimeSpan.FromSeconds(15));

        /// <summary>
        /// Configures a periodic, automatic update based on
        /// <paramref name="retryDelay"/>.
        /// </summary>
        /// <param name="retryDelay">
        /// The interval between configuration updates.
        /// </param>
        /// <returns>
        /// The same instance on which the method was called.
        /// </returns>
        public ConsulConfigurationSource AutoUpdate(TimeSpan retryDelay)
        {
            _retryDelay = retryDelay;

            return this;
        }

        internal ConsulConfigurationSource DoNotAutoUpdate()
        {
            _retryDelay = null;

            return this;
        }

        IConfigurationProvider IConfigurationSource.Build(IConfigurationBuilder builder)
        {
            var consulClient = new ObservableConsul(_consulConfig);

            return Build(consulClient);
        }

        internal IConfigurationProvider Build(IObservableConsul consulClient)
        {
            return new ConsulConfigurationProvider(consulClient, _cache, _consulDependencies, _serviceConfigMappings, _kvTreeConfigMappings, _kvItemConfigMappings, _retryDelay);
        }

        internal ConsulConfigurationSource UseCache(IEmergencyCache cache)
        {
            _cache = cache;

            return this;
        }
    }

    public static class ConsulConfigurationExtensions
    {
        public static IConfigurationBuilder AddConsul(this IConfigurationBuilder builder, Action<ConsulConfigurationSource> configureSource)
        {
            var source = new ConsulConfigurationSource();
            configureSource(source);
            builder.Add(source);

            return builder;
        }
    }
}