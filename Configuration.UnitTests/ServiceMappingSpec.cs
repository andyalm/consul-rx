using System.Linq;
using ConsulRx.TestSupport;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace ConsulRx.Configuration.UnitTests
{
    public class ServiceMappingSpec
    {
        private readonly FakeObservableConsul _consul = new FakeObservableConsul();
        
        [Fact]
        public void ServiceEndpointCanBeRetrievedViaMappedConfigKey()
        {
            var source = new ConsulConfigurationSource()
                .UseCache(new InMemoryEmergencyCache())
                .MapService("myservice1", "serviceEndpoints:v1:myservice", EndpointFormatters.AddressAndPort, NodeSelectors.First);
            
            var consulState = new ConsulState();
            consulState = consulState.UpdateService(new Service
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
            var source = new ConsulConfigurationSource()
                .UseCache(new InMemoryEmergencyCache())
                .MapService("myservice1", "serviceEndpoints:v1:myservice", EndpointFormatters.Http, NodeSelectors.First);
            
            var consulState = new ConsulState();
            consulState = consulState.UpdateService(new Service
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

        [Fact]
        public void ServiceEndpointsCanBeACollection()
        {
            var source = new ConsulConfigurationSource()
                .UseCache(new InMemoryEmergencyCache())
                .MapService("myservice1", "serviceEndpoints:v1:myservice", EndpointFormatters.AddressAndPort, NodeSelectors.All);
            
            var consulState = new ConsulState();
            consulState = consulState.UpdateService(new Service
            {
                Name = "myservice1",
                Nodes = new[]
                {
                    new ServiceNode
                    {
                        Address = "myaddress",
                        Port = 80
                    }, 
                    new ServiceNode
                    {
                        Address = "anotheraddress",
                        Port = 8080
                    } 
                }
            });

            var configProvider = _consul.LoadConfigProvider(source, consulState);

            string serviceEndpoint;
            configProvider.TryGet("serviceEndpoints:v1:myservice:0", out serviceEndpoint).Should().BeTrue();
            serviceEndpoint.Should().Be("myaddress:80");
            
            configProvider.TryGet("serviceEndpoints:v1:myservice:1", out serviceEndpoint).Should().BeTrue();
            serviceEndpoint.Should().Be("anotheraddress:8080");
        }
    }
}
