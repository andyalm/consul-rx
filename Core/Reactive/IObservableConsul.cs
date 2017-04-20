using System;

namespace ConsulRazor.Reactive
{
    public interface IObservableConsul
    {
        IObservable<ServiceObservation> ObserveService(string serviceName);
        IObservable<KeyObservation> ObserveKey(string key);
        IObservable<KeyRecursiveObservation> ObserveKeyRecursive(string prefix);
    }
}