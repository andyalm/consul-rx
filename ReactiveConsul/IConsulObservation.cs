using Consul;

namespace ReactiveConsul
{
    public interface IConsulObservation
    {
        QueryResult Result { get; }
    }
}