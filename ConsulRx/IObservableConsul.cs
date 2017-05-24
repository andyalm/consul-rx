using System;

namespace ConsulRx
{
    public interface IObservableConsul
    {
        IObservable<ServiceObservation> ObserveService(string serviceName);
        IObservable<KeyObservation> ObserveKey(string key);
        IObservable<KeyRecursiveObservation> ObserveKeyRecursive(string prefix);
        IObservable<ConsulState> ObserveDependencies(ConsulDependencies dependencies);
    }
}