using Consul;

namespace ConsulTemplate.Reactive
{
    public interface IConsulObservation
    {
        QueryResult Result { get; }
    }
}