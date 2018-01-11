using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace ConsulRx.TestSupport
{
    public class FakeObservableConsul : IObservableConsul
    {
        public Subject<ServiceObservation> ServiceObservations { get; private set; } = new Subject<ServiceObservation>();
        public HashSet<string> ObservingServices { get; } = new HashSet<string>();
        public IObservable<ServiceObservation> ObserveService(string serviceName)
        {
            ServiceObservations = new Subject<ServiceObservation>();
            ObservingServices.Add(serviceName);

            return ServiceObservations;
        }

        public ConsulState CurrentState { get; set; } = new ConsulState();
        
        public ObservableConsulConfiguration Configuration { get; } = new ObservableConsulConfiguration();
        
        public Task<Service> GetServiceAsync(string serviceName)
        {
            var service = CurrentState.GetService(serviceName);
            return Task.FromResult(service);
        }

        public Subject<KeyObservation> KeyObservations { get; private set; } = new Subject<KeyObservation>();
        public HashSet<string> ObservingKeys { get; } = new HashSet<string>();
        public IObservable<KeyObservation> ObserveKey(string key)
        {
            KeyObservations = new Subject<KeyObservation>();
            ObservingKeys.Add(key);

            return KeyObservations;
        }

        public Task<KeyValueNode> GetKeyAsync(string key)
        {
            var value = CurrentState.KVStore.GetValue(key);

            if (value == null)
            {
                return null;
            }
            
            return Task.FromResult(new KeyValueNode(key, value));
        }

        public Subject<KeyRecursiveObservation> KeyRecursiveObservations { get; private set; } = new Subject<KeyRecursiveObservation>();
        public HashSet<string> ObservingKeyPrefixes { get; } = new HashSet<string>();
        public IObservable<KeyRecursiveObservation> ObserveKeyRecursive(string prefix)
        {
            KeyRecursiveObservations = new Subject<KeyRecursiveObservation>();
            ObservingKeyPrefixes.Add(prefix);

            return KeyRecursiveObservations;
        }

        public Task<IEnumerable<KeyValueNode>> GetKeyRecursiveAsync(string prefix)
        {
            var values = CurrentState.KVStore.GetTree(prefix);

            return Task.FromResult(values);
        }

        public Subject<ConsulState> DependencyObservations { get; private set; } = new Subject<ConsulState>();
        public List<ConsulDependencies> ObservingDependencies { get; } = new List<ConsulDependencies>();

        public IObservable<ConsulState> ObserveDependencies(ConsulDependencies dependencies)
        {
            DependencyObservations = new Subject<ConsulState>();
            ObservingDependencies.Add(dependencies);

            return DependencyObservations;
        }

        public Task<ConsulState> GetDependenciesAsync(ConsulDependencies dependencies)
        {
            return Task.FromResult(CurrentState);
        }
    }
}