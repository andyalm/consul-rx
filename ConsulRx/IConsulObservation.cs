using Consul;

namespace ConsulRx
{
    public interface IConsulObservation
    {
        QueryResult Result { get; }
    }
}