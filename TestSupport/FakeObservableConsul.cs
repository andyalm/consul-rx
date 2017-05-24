using System;
using System.Collections.Generic;
using System.Reactive.Subjects;

namespace ConsulRx.TestSupport
{
    public class FakeObservableConsul : IObservableConsul
    {
        public Subject<ServiceObservation> Services { get; } = new Subject<ServiceObservation>();
        public HashSet<string> ObservingServices { get; } = new HashSet<string>();
        public IObservable<ServiceObservation> ObserveService(string serviceName)
        {
            ObservingServices.Add(serviceName);

            return Services;
        }

        public Subject<KeyObservation> Keys { get; } = new Subject<KeyObservation>();
        public HashSet<string> ObservingKeys { get; } = new HashSet<string>();
        public IObservable<KeyObservation> ObserveKey(string key)
        {
            ObservingKeys.Add(key);

            return Keys;
        }

        public Subject<KeyRecursiveObservation> KeysRecursive { get; } = new Subject<KeyRecursiveObservation>();
        public HashSet<string> ObservingKeyPrefixes { get; } = new HashSet<string>();
        public IObservable<KeyRecursiveObservation> ObserveKeyRecursive(string prefix)
        {
            ObservingKeyPrefixes.Add(prefix);

            return KeysRecursive;
        }
        
        public Subject<ConsulState> Dependencies { get; } = new Subject<ConsulState>();
        public List<ConsulDependencies> ObservingDependencies { get; } = new List<ConsulDependencies>();

        public IObservable<ConsulState> ObserveDependencies(ConsulDependencies dependencies)
        {
            ObservingDependencies.Add(dependencies);

            return Dependencies;
        }
    }
}