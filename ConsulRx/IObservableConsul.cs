using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ConsulRx
{
    public interface IObservableConsul
    {
        IObservable<ServiceObservation> ObserveService(string serviceName);
        Task<Service> GetServiceAsync(string serviceName);
        
        IObservable<KeyObservation> ObserveKey(string key);
        Task<KeyValueNode> GetKeyAsync(string key);
        
        IObservable<KeyRecursiveObservation> ObserveKeyRecursive(string prefix);
        Task<IEnumerable<KeyValueNode>> GetKeyRecursiveAsync(string prefix);
        
        IObservable<ConsulState> ObserveDependencies(ConsulDependencies dependencies);
        Task<ConsulState> GetDependenciesAsync(ConsulDependencies dependencies);
    }
}