using ConsulRx.TestSupport;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace ConsulRx.Configuration.UnitTests
{
    public class KVTreeMappingSpec
    {
        private readonly FakeObservableConsul _consul = new FakeObservableConsul();
        
        [Fact]
        public void TreeOfKeysBeRetrievedViaMappedConfigPrefix()
        {
            var source = new ConsulConfigurationSource()
                .UseCache(new InMemoryEmergencyCache())
                .MapKeyPrefix("apps/myapp", "consul");
            
            var consulState = new ConsulState();
            consulState = consulState.UpdateKVNodes(new[]
            {
                new KeyValueNode("apps/myapp/folder1/item1", "value1"),
                new KeyValueNode("apps/myapp/folder1/item2", "value2"),
                new KeyValueNode("apps/myapp/folder2/item1", "value3")
            });

            var configProvider = _consul.LoadConfigProvider(source, consulState);
            
            VerifyConfigKey(configProvider, "consul:folder1:item1", "value1");
            VerifyConfigKey(configProvider, "consul:folder1:item2", "value2");
            VerifyConfigKey(configProvider, "consul:folder2:item1", "value3");
        }
        
        [Fact]
        public void IndividualKeyBeRetrievedViaMappedConfigKey()
        {
            var source = new ConsulConfigurationSource()
                .UseCache(new InMemoryEmergencyCache())
                .MapKey("apps/myapp/myfeature", "consul:afeature");
            
            var consulState = new ConsulState();
            consulState = consulState.UpdateKVNode(new KeyValueNode("apps/myapp/myfeature", "myvalue"));

            var configProvider = _consul.LoadConfigProvider(source, consulState);
            
            VerifyConfigKey(configProvider, "consul:afeature", "myvalue");
        }

        private void VerifyConfigKey(IConfigurationProvider configProvider, string key, string expectedValue)
        {
            configProvider.TryGet(key, out var actualValue).Should().BeTrue();
            actualValue.Should().Be(expectedValue);
        }
    }
}