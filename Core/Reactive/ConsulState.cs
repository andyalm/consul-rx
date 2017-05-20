using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using ConsulRazor.Templating;

namespace ConsulRazor.Reactive
{
    public class ConsulState : IDisposable
    {
        public ChangeTrackingCollection<Service> Services { get; }
        public KeyValueStore KVStore { get; }
        public ChangeTrackingCollection<string> MissingKeyPrefixes { get; }
        public IObservable<ConsulState> Changes { get; }
        private readonly ConcurrentBag<IDisposable> _subscriptions = new ConcurrentBag<IDisposable>();

        public ConsulState()
        {
            Services = new ChangeTrackingCollection<Service>(s => s.Name);
            KVStore = new KeyValueStore();
            MissingKeyPrefixes = new ChangeTrackingCollection<string>(v => v);

            Changes = Services.Changes.Select(_ => this).Merge(KVStore.Changes.Select(_ => this)).Merge(MissingKeyPrefixes.Changes.Select(_ => this));
        }

        private void UpdateService(Service service)
        {
            Services.TryUpdate(service);
        }

        private void UpdateKVNode(KeyValueNode kvNode)
        {
            KVStore.Update(kvNode);
        }

        private void UpdateKVNodes(IEnumerable<KeyValueNode> kvNodes)
        {
            KVStore.Update(kvNodes);
        }

        private void MarkKeyPrefixAsMissingOrEmpty(string keyPrefix)
        {
            MissingKeyPrefixes.TryUpdate(keyPrefix);
        }

        public bool SatisfiesAll(ConsulDependencies consulDependencies)
        {
            return consulDependencies.Services.IsSubsetOf(Services.Select(s => s.Name))
                   && consulDependencies.Keys.IsSubsetOf(KVStore.Select(s => s.FullKey))
                   && consulDependencies.KeyPrefixes.All(p => KVStore.Any(k => k.FullKey.StartsWith(p)) || MissingKeyPrefixes.Contains(p));
        }

        public IDisposable ObserveAll(ConsulDependencies dependencies, IObservableConsul client)
        {
            var subscriptions = new CompositeDisposable();
            
            subscriptions.Add(Observe(() => client.ObserveServices(dependencies.Services),
                services => UpdateService(services.ToService())));
            
            subscriptions.Add(Observe(() => client.ObserveKeys(dependencies.Keys),
                kv => UpdateKVNode(kv.ToKeyValueNode())));
            
            subscriptions.Add(Observe(() => client.ObserveKeysRecursive(dependencies.KeyPrefixes),
                kv =>
                {
                    if (kv.Result.Response == null || !kv.Result.Response.Any())
                        MarkKeyPrefixAsMissingOrEmpty(kv.KeyPrefix);
                    else
                        UpdateKVNodes(kv.ToKeyValueNodes());
                }));

            return subscriptions;
        }
        
        private IDisposable Observe<T>(Func<IObservable<T>> getObservable, Action<T> subscribe) where T : IConsulObservation
        {
            return getObservable().Subscribe(item => HandleConsulObservable(item, subscribe));
        }
        
        private void HandleConsulObservable<T>(T observation, Action<T> action) where T : IConsulObservation
        {
            if (observation.Result.StatusCode == HttpStatusCode.OK ||
                observation.Result.StatusCode == HttpStatusCode.NotFound)
            {
                try
                {
                    action(observation);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    throw;
                }
            }
            else
            {
                Console.WriteLine($"Error retrieving something: {observation.Result.StatusCode}");
            }
        }

        public void Dispose()
        {
            foreach (var subscription in _subscriptions)
            {
                subscription.Dispose();
            }
        }
    }
}