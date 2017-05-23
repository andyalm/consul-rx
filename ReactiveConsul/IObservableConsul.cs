using System;
using System.Threading.Tasks;

namespace ReactiveConsul
{
    public interface IObservableConsul
    {
        IObservable<ServiceObservation> ObserveService(string serviceName);
        IObservable<KeyObservation> ObserveKey(string key);
        IObservable<KeyRecursiveObservation> ObserveKeyRecursive(string prefix);
        IObservable<ConsulState> ObserveDependencies(ConsulDependencies dependencies);
    }
}