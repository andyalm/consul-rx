using Consul;

namespace ConsulRazor.Reactive
{
    public interface IConsulObservation
    {
        QueryResult Result { get; }
    }
}