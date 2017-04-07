using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Consul;

namespace ConsulTemplateDotNet.Reactive
{
    public class ObservableConsul
    {
        private readonly ConsulClient _client;
        private readonly ObservableConsulConfiguration _config;

        public ObservableConsul(ConsulClient client, ObservableConsulConfiguration config = null)
        {
            _client = client;
            _config = config ?? new ObservableConsulConfiguration();
        }

        public IObservable<CatalogService[]> ObserveService(string serviceName)
        {
            return LongPoll(index => _client.Catalog.Service(serviceName, null,
                QueryOptions(index)));
        }

        public IObservable<KVPair> ObserveKey(string key)
        {
            return LongPoll(index => _client.KV.Get(key, QueryOptions(index)));
        }

        public IObservable<KVPair> ObserveKeys(params string[] keys)
        {
            return ObserveKeys((IEnumerable<string>)keys);
        }

        public IObservable<KVPair> ObserveKeys(IEnumerable<string> keys)
        {
            return keys.Select(ObserveKey).Merge();
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
                WaitTime = _config.WaitTime
            };
        }

        private IObservable<T> LongPoll<T>(Func<ulong,Task<QueryResult<T>>> poll)
        {
            ulong index = default(ulong);
            return Observable.FromAsync(async () =>
            {
                var result = await poll(index);
                index = result.LastIndex;

                return result.Response;
            }).Repeat();
        }
    }
}