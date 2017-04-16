using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Consul;

namespace ConsulTemplate.Reactive
{
    public class ObservableConsul : IObservableConsul
    {
        private readonly ConsulClient _client;
        private readonly TimeSpan? _longPollMaxWait;
        private readonly string _aclToken; 

        public ObservableConsul(ConsulClient client, TimeSpan? longPollMaxWait = null, string aclToken = null)
        {
            _client = client;
            _longPollMaxWait = longPollMaxWait;
            _aclToken = aclToken;
        }

        public ObservableConsul(ObservableConsulConfiguration config)
        {
            _client = new ConsulClient(c =>
            {
                c.Address = new Uri(config.Endpoint ?? "http://localhost:8500");
                c.Datacenter = config.Datacenter;
                c.Token = config.GossipToken;
            });
            _longPollMaxWait = config.LongPollMaxWait;
            _aclToken = config.AclToken;
        }

        public IObservable<ServiceObservation> ObserveService(string serviceName)
        {
            return LongPoll(index => _client.Catalog.Service(serviceName, null,
                QueryOptions(index)), result => new ServiceObservation(serviceName, result));
        }

        public IObservable<KeyObservation> ObserveKey(string key)
        {
            return LongPoll(index => _client.KV.Get(key, QueryOptions(index)), result => new KeyObservation(key, result));
        }

        public IObservable<KeyRecursiveObservation> ObserveKeyRecursive(string prefix)
        {
            return LongPoll(index => _client.KV.List(prefix, QueryOptions(index)), result => new KeyRecursiveObservation(prefix, result));
        }   

        private QueryOptions QueryOptions(ulong index)
        {
            return new QueryOptions
            {
                Token = _aclToken ?? "anonymous",
                WaitIndex = index,
                WaitTime = _longPollMaxWait
            };
        }

        private IObservable<TObservation> LongPoll<TResponse,TObservation>(Func<ulong,Task<QueryResult<TResponse>>> poll, Func<QueryResult<TResponse>,TObservation> toObservation)
        {
            ulong index = default(ulong);
            return Observable.FromAsync(async () =>
            {
                try
                {
                    var result = await poll(index);
                    index = result.LastIndex;

                    return toObservation(result);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    throw;
                }
            }).Repeat();
        }
    }

    public static class ObservableConsulExtensions
    {
        public static IObservable<KeyObservation> ObserveKeys(this IObservableConsul client, params string[] keys)
        {
            return client.ObserveKeys((IEnumerable<string>)keys);
        }

        public static IObservable<KeyObservation> ObserveKeys(this IObservableConsul client, IEnumerable<string> keys)
        {
            return keys.Select(client.ObserveKey).Merge();
        }

        public static IObservable<KeyRecursiveObservation> ObserveKeysRecursive(this IObservableConsul client, IEnumerable<string> prefixes)
        {
            return prefixes.Select(client.ObserveKeyRecursive).Merge();
        }

        public static IObservable<ServiceObservation> ObserveServices(this IObservableConsul client, IEnumerable<string> serviceNames)
        {
            return serviceNames.Select(client.ObserveService).Merge();
        }

        public static IObservable<ServiceObservation> ObserveServices(this IObservableConsul client, params string[] serviceNames)
        {
            return client.ObserveServices((IEnumerable<string>)serviceNames);
        }
    }
}