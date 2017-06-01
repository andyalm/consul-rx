using System;
using System.Collections.Generic;
using System.Reactive.Subjects;

namespace ConsulRx.TestSupport
{
    public class FakeObservableConsul : IObservableConsul
    {
        public Subject<ServiceObservation> Services { get; private set; } = new Subject<ServiceObservation>();
        public HashSet<string> ObservingServices { get; } = new HashSet<string>();
        public IObservable<ServiceObservation> ObserveService(string serviceName)
        {
            Services = new Subject<ServiceObservation>();
            ObservingServices.Add(serviceName);

            return Services;
        }

        public Subject<KeyObservation> Keys { get; private set; } = new Subject<KeyObservation>();
        public HashSet<string> ObservingKeys { get; } = new HashSet<string>();
        public IObservable<KeyObservation> ObserveKey(string key)
        {
            Keys = new Subject<KeyObservation>();
            ObservingKeys.Add(key);

            return Keys;
        }

        public Subject<KeyRecursiveObservation> KeysRecursive { get; private set; } = new Subject<KeyRecursiveObservation>();
        public HashSet<string> ObservingKeyPrefixes { get; } = new HashSet<string>();
        public IObservable<KeyRecursiveObservation> ObserveKeyRecursive(string prefix)
        {
            KeysRecursive = new Subject<KeyRecursiveObservation>();
            ObservingKeyPrefixes.Add(prefix);

            return KeysRecursive;
        }
        
        public Subject<ConsulState> Dependencies { get; private set; } = new Subject<ConsulState>();
        public List<ConsulDependencies> ObservingDependencies { get; } = new List<ConsulDependencies>();

        public IObservable<ConsulState> ObserveDependencies(ConsulDependencies dependencies)
        {
            Dependencies = new Subject<ConsulState>();
            ObservingDependencies.Add(dependencies);

            return Dependencies;
        }
    }
}