using System.Net;

namespace ConsulTemplate.Reactive
{
    public class ConsulServiceNotFoundException : ConsulApiException
    {
        public ConsulServiceNotFoundException(string serviceName) : base((HttpStatusCode) HttpStatusCode.NotFound, (string) $"Consul service {serviceName} not found")
        {
        }
    }
}