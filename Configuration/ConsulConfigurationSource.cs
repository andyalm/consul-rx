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
        private IEmergencyCache _cache = NullEmergencyCache.Instance;
        private readonly ConsulRxOptions _options = new ConsulRxOptions();
        private bool _autoUpdateOptionsApplied = false;

        public ConsulConfigurationSource()
        {
            var autoUpdateEnv = Environment.GetEnvironmentVariable("CONSULRX_AUTO_UPDATE");
            if (autoUpdateEnv != null && IsFalsey(autoUpdateEnv))
            {
                _options.AutoUpdate = false;
            }
        }

        public ConsulConfigurationSource Configure(Action<ConsulRxOptions> configureOptions)
        {
            configureOptions(_options);
            return this;
        }

        private static bool IsFalsey(string value)
        {
            return value.Length == 0
                || value.Equals("0")
                || value.Equals("false", StringComparison.OrdinalIgnoreCase);
        }
        
        public ConsulConfigurationSource Endpoint(string consulEndpoint)
        {
            _consulConfig.Endpoint = consulEndpoint;

            return this;
        }

        public ConsulConfigurationSource Endpoint(string consulEndpoint, string aclToken)
        {
            _consulConfig.Endpoint = consulEndpoint;
            _consulConfig.AclToken = aclToken;

            return this;
        }

        public ConsulConfigurationSource ConsistencyMode(ConsistencyMode consistencyMode)
        {
            _consulConfig.ConsistencyMode = consistencyMode;

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
            return MapKey<PassthruConfigTypeConverter>(consulKey, configKey);
        }

        public ConsulConfigurationSource MapKey<TTypeConverter>(string consulKey, string configKey) where TTypeConverter : IConfigTypeConverter, new()
        {
            _consulDependencies.Keys.Add(consulKey);
            _kvItemConfigMappings.Add(new KVItemConfigMapping(configKey, consulKey, new TTypeConverter()));

            return this;
        }

        /// <summary>
        /// Configures a periodic, automatic update based on
        /// <paramref name="options"/>.
        /// </summary>
        /// <param name="options">
        /// The settings for automatic updates.
        /// </param>
        /// <returns>
        /// The same instance on which the method was called.
        /// </returns>
        public ConsulConfigurationSource AutoUpdate(AutoUpdateOptions options = null)
        {
            _options.AutoUpdate = true;
            options = options ?? new AutoUpdateOptions();
            _autoUpdateOptionsApplied = true;

            _consulConfig.RetryDelay = options.ErrorRetryInterval;
            _consulConfig.LongPollMaxWait = options.UpdateMaxInterval;

            return this;
        }

        IConfigurationProvider IConfigurationSource.Build(IConfigurationBuilder builder)
        {
            var consulClient = new ObservableConsul(_consulConfig);

            return Build(consulClient);
        }

        internal IConfigurationProvider Build(IObservableConsul consulClient)
        {
            if (_options.AutoUpdate && !_autoUpdateOptionsApplied)
            {
                var defaults = new AutoUpdateOptions();
                _consulConfig.RetryDelay = defaults.ErrorRetryInterval;
                _consulConfig.LongPollMaxWait = defaults.UpdateMaxInterval;
            }

            return new ConsulConfigurationProvider(consulClient, _cache, _consulDependencies, _serviceConfigMappings, _kvTreeConfigMappings, _kvItemConfigMappings, _options.AutoUpdate);
        }

        public ConsulConfigurationSource UseFilesystemCache()
        {
            return UseCache(new FileSystemEmergencyCache());
        }

        public ConsulConfigurationSource UseCache(IEmergencyCache cache)
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