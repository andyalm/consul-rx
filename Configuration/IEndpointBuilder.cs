using System;
using System.Linq;

namespace ConsulRx.Configuration
{
    public interface IEndpointBuilder
    {
        string BuildEndpoint(Service service);
    }

    public class SimpleEndpointBuilder : IEndpointBuilder
    {
        private static readonly Random _random = new Random();
        
        public virtual string BuildEndpoint(Service service)
        {
            if (!service.Nodes.Any())
                return null;

            var randomNodeIndex = _random.Next(service.Nodes.Length - 1);
            var node = service.Nodes[randomNodeIndex];

            return $"{node.Address}:{node.Port}";
        }
    }

    public class LambdaEndpointBuilder : IEndpointBuilder
    {
        private readonly Func<Service, string> _buildEndpoint;

        public LambdaEndpointBuilder(Func<Service, string> buildEndpoint)
        {
            _buildEndpoint = buildEndpoint;
        }

        public string BuildEndpoint(Service service)
        {
            return _buildEndpoint(service);
        }
    }

    public class HttpEndpointBuilder : SimpleEndpointBuilder
    {
        private readonly string _scheme;

        public HttpEndpointBuilder(string scheme = "http")
        {
            _scheme = scheme;
        }

        public override string BuildEndpoint(Service service)
        {
            var host = base.BuildEndpoint(service);

            return $"{_scheme}://{host}";
        }
    }
}