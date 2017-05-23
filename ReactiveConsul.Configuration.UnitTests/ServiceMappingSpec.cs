using System.Linq;
using FluentAssertions;
using ReactiveConsul.TestSupport;
using Xunit;

namespace ReactiveConsul.Configuration.UnitTests
{
    public class ServiceMappingSpec
    {
        private readonly FakeObservableConsul _consul = new FakeObservableConsul();
        
        [Fact]
        public void ServiceEndpointCanBeRetrievedViaMappedConfigKey()
        {
            var source = new ConsulConfigurationSource();
            source.MapService("myservice1", "serviceEndpoints:v1:myservice");
            
            var consulState = new ConsulState();
            consulState.Services.TryUpdate(new Service
            {
                Name = "myservice1",
                Nodes = new[]
                {
                    new ServiceNode
                    {
                        Address = "myaddress",
                        Port = 80
                    }, 
                }
            });

            var configProvider = _consul.LoadConfigProvider(source, consulState);
            
            string serviceEndpoint;
            configProvider.TryGet("serviceEndpoints:v1:myservice", out serviceEndpoint).Should().BeTrue();
            serviceEndpoint.Should().Be("myaddress:80");
        }
        
        [Fact]
        public void ServiceEndpointBuildingCanBeCustomized()
        {
            var source = new ConsulConfigurationSource();
            source.MapService("myservice1", "serviceEndpoints:v1:myservice", new LambdaEndpointBuilder(s => $"http://{s.Nodes.First().Address}"));
            
            var consulState = new ConsulState();
            consulState.Services.TryUpdate(new Service
            {
                Name = "myservice1",
                Nodes = new[]
                {
                    new ServiceNode
                    {
                        Address = "myaddress",
                        Port = 80
                    }, 
                }
            });

            var configProvider = _consul.LoadConfigProvider(source, consulState);
            
            string serviceEndpoint;
            configProvider.TryGet("serviceEndpoints:v1:myservice", out serviceEndpoint).Should().BeTrue();
            serviceEndpoint.Should().Be("http://myaddress");
        }
    }
}
