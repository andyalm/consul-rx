using System;
using System.Collections.Generic;
using Consul;

namespace ConsulTemplate.Reactive
{
    public interface IObservableConsul
    {
        IObservable<ServiceObservation> ObserveService(string serviceName);
        IObservable<KeyObservation> ObserveKey(string key);
        IObservable<KeyRecursiveObservation> ObserveKeyRecursive(string prefix);
    }
}