using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace ConsulRx.Configuration
{
    public class ConsulConfigurationProvider : ConfigurationProvider
    {
        private readonly IObservableConsul _consulClient;
        private readonly ConsulDependencies _dependencies;
        private readonly ServiceConfigMappingCollection _serviceConfigMappings;
        private readonly KVTreeConfigMappingCollection _kvTreeConfigMappings;
        private readonly KVItemConfigMappingCollection _kvItemConfigMappings;
        private ConsulState _consulState;

        public ConsulConfigurationProvider(IObservableConsul consulClient, ConsulDependencies dependencies, ServiceConfigMappingCollection serviceConfigMappings, KVTreeConfigMappingCollection kvTreeConfigMappings, KVItemConfigMappingCollection kvItemConfigMappings)
        {
            _consulClient = consulClient;
            _dependencies = dependencies;
            _serviceConfigMappings = serviceConfigMappings;
            _kvTreeConfigMappings = kvTreeConfigMappings;
            _kvItemConfigMappings = kvItemConfigMappings;
        }

        public override void Load()
        {
            LoadAsync().GetAwaiter().GetResult();
        }

        private Task LoadAsync()
        {
            var stateLoaded = new TaskCompletionSource<ConsulState>();
            var loaded = false;
            _consulClient.ObserveDependencies(_dependencies).Subscribe(consulState =>
            {
                _consulState = consulState;
                UpdateData();
                if (!loaded)
                {
                    //TODO: Probable race condition here.
                    loaded = true;
                    stateLoaded.SetResult(consulState);
                }
                else
                {
                    OnReload();
                }
            }, exception =>
            {
                stateLoaded.SetException(exception);
            });

            return stateLoaded.Task;
        }

        private void UpdateData()
        {
            var data = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
            AddServiceData(data);
            AddKVTreeData(data);
            AddKVItemData(data);

            Data = data;
        }

        private void AddKVItemData(Dictionary<string, string> data)
        {
            foreach (var mapping in _kvItemConfigMappings)
            {
                var value = _consulState.KVStore.GetValue(mapping.ConsulKey);
                data[mapping.ConfigKey] = value;
            }
        }

        private void AddServiceData(Dictionary<string, string> data)
        {
            foreach (var mapping in _serviceConfigMappings)
            {
                var service = _consulState.GetService(mapping.ServiceName);
                if(service != null)
                {
                    var endpoint = mapping.EndpointBuilder.BuildEndpoint(service);
                    data[mapping.ConfigKey] = endpoint;
                }
            }
        }

        private void AddKVTreeData(Dictionary<string, string> data)
        {
            foreach (var mapping in _kvTreeConfigMappings)
            {
                foreach (var kv in _consulState.KVStore.GetTree(mapping.ConsulKeyPrefix))
                {
                    var fullConfigKey = mapping.FullConfigKey(kv);
                    data[fullConfigKey] = kv.Value;
                }
            }
        }
    }
}