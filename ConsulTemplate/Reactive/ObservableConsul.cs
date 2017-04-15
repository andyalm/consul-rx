using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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

        public IObservable<CatalogService[]> ObserveService(string serviceName)
        {
            return LongPoll(index => _client.Catalog.Service(serviceName, null,
                QueryOptions(index)), result => HandleServiceError(result, serviceName));
        }

        public IObservable<KVPair> ObserveKey(string key)
        {
            return LongPoll(index => _client.KV.Get(key, QueryOptions(index)), result => HandleKVError(result, key));
        }

        public IObservable<KVPair[]> ObserveKeyRecursive(string prefix)
        {
            return LongPoll(index => _client.KV.List(prefix, QueryOptions(index)), result => HandleKVError(result, prefix));
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

        private CatalogService[] HandleServiceError(QueryResult<CatalogService[]> result, string serviceName)
        {
            if (result.StatusCode == HttpStatusCode.NotFound)
            {
                throw new ConsulServiceNotFoundException(serviceName);
            }
            
            throw new ConsulApiException(result.StatusCode, $"Unexpected HTTP response {result.StatusCode} while trying to get service '{serviceName}'");
        }

        private T HandleKVError<T>(QueryResult<T> result, string key)
        {
            if (result.StatusCode == HttpStatusCode.NotFound)
            {
                throw new ConsulKeyNotFoundException(key);
            }

            throw new ConsulApiException(result.StatusCode, $"Unexpected HTTP response {result.StatusCode} while trying to get key '{key}'");
        }

        private IObservable<T> LongPoll<T>(Func<ulong,Task<QueryResult<T>>> poll, Func<QueryResult<T>,T> onError)
        {
            ulong index = default(ulong);
            return Observable.FromAsync(async () =>
            {
                try
                {
                    var result = await poll(index);
                    index = result.LastIndex;

                    if (result.StatusCode == HttpStatusCode.OK)
                    {
                        return result.Response;
                    }

                    return onError(result);
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
        public static IObservable<KVPair> ObserveKeys(this IObservableConsul client, params string[] keys)
        {
            return client.ObserveKeys((IEnumerable<string>)keys);
        }

        public static IObservable<KVPair> ObserveKeys(this IObservableConsul client, IEnumerable<string> keys)
        {
            return keys.Select(client.ObserveKey).Merge();
        }

        public static IObservable<KVPair[]> ObserveKeysRecursive(this IObservableConsul client, IEnumerable<string> prefixes)
        {
            return prefixes.Select(client.ObserveKeyRecursive).Merge();
        }

        public static IObservable<CatalogService[]> ObserveServices(this IObservableConsul client, IEnumerable<string> serviceNames)
        {
            return serviceNames.Select(client.ObserveService).Merge();
        }

        public static IObservable<CatalogService[]> ObserveServices(this IObservableConsul client, params string[] serviceNames)
        {
            return client.ObserveServices((IEnumerable<string>)serviceNames);
        }
    }
}