using System.Net;

namespace ConsulTemplate.Reactive
{
    public class ConsulKeyNotFoundException : ConsulApiException
    {
        public ConsulKeyNotFoundException(string key) : base((HttpStatusCode) HttpStatusCode.NotFound, (string) $"Consul key {key} not found")
        {
        }
    }
}