using System;
using Microsoft.Extensions.Configuration;

namespace ReactiveConsul.Configuration
{
    public class ConsulConfigurationSource : IConfigurationSource
    {
        private readonly ObservableConsulConfiguration _consulConfig = new ObservableConsulConfiguration();
        private readonly ConsulDependencies _consulDependencies = new ConsulDependencies();
        private readonly ServiceConfigMappingCollection _serviceConfigMappings = new ServiceConfigMappingCollection();
        private readonly KVTreeConfigMappingCollection _kvTreeConfigMappings = new KVTreeConfigMappingCollection();
        private readonly KVItemConfigMappingCollection _kvItemConfigMappings = new KVItemConfigMappingCollection();

        public ConsulConfigurationSource Endpoint(string consulEndpoint)
        {
            _consulConfig.Endpoint = consulEndpoint;

            return this;
        }

        public ConsulConfigurationSource MapService(string consulServiceName, string configKey,
            Func<Service, string> endpointBuilder)
        {
            return MapService(consulServiceName, configKey, new LambdaEndpointBuilder(endpointBuilder));
        }

        public ConsulConfigurationSource MapHttpService(string consulServiceName, string configKey, string scheme = "http")
        {
            return MapService(consulServiceName, configKey, new HttpEndpointBuilder(scheme));
        }

        public ConsulConfigurationSource MapService(string consulServiceName, string configKey, IEndpointBuilder endpointBuilder = null)
        {
            _consulDependencies.Services.Add(consulServiceName);
            _serviceConfigMappings.Add(new ServiceConfigMapping(configKey, consulServiceName, endpointBuilder ?? new SimpleEndpointBuilder()));
            
            return this;
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

        IConfigurationProvider IConfigurationSource.Build(IConfigurationBuilder builder)
        {
            var consulClient = new ObservableConsul(_consulConfig);

            return Build(consulClient);
        }

        internal IConfigurationProvider Build(IObservableConsul consulClient)
        {
            return new ConsulConfigurationProvider(consulClient, _consulDependencies, _serviceConfigMappings, _kvTreeConfigMappings, _kvItemConfigMappings);
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