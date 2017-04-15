using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Consul;

namespace ConsulTemplate.Reactive
{
    public class ObservableConsul
    {
        private readonly ConsulClient _client;
        private readonly TimeSpan? _longPollMaxWait;

        public ObservableConsul(ConsulClient client, TimeSpan? longPollMaxWait)
        {
            _client = client;
            _longPollMaxWait = longPollMaxWait;
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

        public IObservable<KVPair> ObserveKeys(params string[] keys)
        {
            return ObserveKeys((IEnumerable<string>)keys);
        }

        public IObservable<KVPair> ObserveKeys(IEnumerable<string> keys)
        {
            return keys.Select(ObserveKey).Merge();
        }

        public IObservable<KVPair[]> ObserveKeysRecursive(IEnumerable<string> prefixes)
        {
            return prefixes.Select(ObserveKeyRecursive).Merge();
        }

        public IObservable<CatalogService[]> ObserveServices(IEnumerable<string> serviceNames)
        {
            return serviceNames.Select(ObserveService).Merge();
        }

        public IObservable<CatalogService[]> ObserveServices(params string[] serviceNames)
        {
            return ObserveServices((IEnumerable<string>)serviceNames);
        }

        private QueryOptions QueryOptions(ulong index)
        {
            return new QueryOptions
            {
                Token = "anonymous",
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
}