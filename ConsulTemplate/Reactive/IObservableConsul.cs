using System;
using System.Collections.Generic;
using Consul;

namespace ConsulTemplate.Reactive
{
    public interface IObservableConsul
    {
        IObservable<CatalogService[]> ObserveService(string serviceName);
        IObservable<KVPair> ObserveKey(string key);
        IObservable<KVPair[]> ObserveKeyRecursive(string prefix);
    }
}