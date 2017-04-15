using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using Consul;
using ConsulTemplate.Reactive;

namespace ConsulTemplate.UnitTests.Support
{
    public class FakeObservableConsul : IObservableConsul
    {
        public Subject<CatalogService[]> Services { get; } = new Subject<CatalogService[]>();
        public List<string> ObservingServices { get; } = new List<string>();
        public IObservable<CatalogService[]> ObserveService(string serviceName)
        {
            ObservingServices.Add(serviceName);

            return Services;
        }

        public Subject<KVPair> Keys { get; } = new Subject<KVPair>();
        public List<string> ObservingKeys { get; } = new List<string>();
        public IObservable<KVPair> ObserveKey(string key)
        {
            ObservingKeys.Add(key);

            return Keys;
        }

        public Subject<KVPair[]> KeysRecursive { get; } = new Subject<KVPair[]>();
        public List<string> ObservingKeyPrefixes { get; } = new List<string>();
        public IObservable<KVPair[]> ObserveKeyRecursive(string prefix)
        {
            ObservingKeyPrefixes.Add(prefix);

            return KeysRecursive;
        }
    }
}