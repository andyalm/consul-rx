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
        IObservable<KVPair> ObserveKeys(params string[] keys);
        IObservable<KVPair> ObserveKeys(IEnumerable<string> keys);
        IObservable<KVPair[]> ObserveKeysRecursive(IEnumerable<string> prefixes);
        IObservable<CatalogService[]> ObserveServices(IEnumerable<string> serviceNames);
        IObservable<CatalogService[]> ObserveServices(params string[] serviceNames);
    }
}