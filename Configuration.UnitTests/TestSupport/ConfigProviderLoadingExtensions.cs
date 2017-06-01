using System;
using System.Threading.Tasks;
using ConsulRx.TestSupport;
using Microsoft.Extensions.Configuration;

namespace ConsulRx.Configuration.UnitTests
{
    public static class ConfigProviderLoadingExtensions
    {
        public static IConfigurationProvider LoadConfigProvider(this FakeObservableConsul consul, ConsulConfigurationSource configSource, ConsulState consulState = null)
        {
            var configProvider = (ConsulConfigurationProvider)configSource.Build(consul);
            
            Task.WhenAll(configProvider.LoadAsync(), Task.Run(() =>
            {
                consul.Dependencies.OnNext(consulState);                    
            })).GetAwaiter().GetResult();

            return configProvider;
        }
    }
}