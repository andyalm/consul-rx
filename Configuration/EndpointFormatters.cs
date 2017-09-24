using System;
using Consul;

namespace ConsulRx.Configuration
{
    public static class EndpointFormatters
    {
        public static Func<ServiceNode, string> AddressAndPort { get; } = node => $"{node.Address}:{node.Port}";

        public static Func<ServiceNode, string> Uri(string scheme = "http")
        {
            return node => $"{scheme}://{node.Address}:{node.Port}";
        }

        public static Func<ServiceNode, string> Http { get; } = Uri();
        public static Func<ServiceNode, string> Https { get; } = Uri("https");
    }
}