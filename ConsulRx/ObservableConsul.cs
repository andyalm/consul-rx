using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Consul;

namespace ConsulRx
{
    public class ObservableConsul : IObservableConsul
    {
        private readonly IConsulClient _client;
        private readonly TimeSpan? _longPollMaxWait;
        private readonly TimeSpan? _retryDelay;
        private readonly string _aclToken; 

        public ObservableConsul(IConsulClient client, TimeSpan? longPollMaxWait = null, TimeSpan? retryDelay = null, string aclToken = null)
        {
            _client = client;
            _longPollMaxWait = longPollMaxWait;
            _retryDelay = retryDelay;
            _aclToken = aclToken;
        }

        public ObservableConsul(ObservableConsulConfiguration config)
        {
            if(config == null)
                throw new ArgumentNullException(nameof(config));

            _client = new ConsulClient(c =>
            {
                c.Address = new Uri(config.Endpoint ?? "http://localhost:8500");
                c.Datacenter = config.Datacenter;
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

        public IObservable<ConsulState> ObserveDependencies(ConsulDependencies dependencies)
        {
            void Observe<T>(Func<IObservable<T>> getObservable, Action<T> subscribe) where T : IConsulObservation
            {
                getObservable().Subscribe(item => HandleConsulObservable(item, subscribe));
            }
        
            void HandleConsulObservable<T>(T observation, Action<T> action) where T : IConsulObservation
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

            var subject = new Subject<ConsulState>();
            var consulState = new ConsulState();
            consulState.Changes.Where(s => s.SatisfiesAll(dependencies)).Subscribe(s => subject.OnNext(s));
            Observe(() => this.ObserveServices(dependencies.Services),
                services => consulState.UpdateService(services.ToService()));
            Observe(() => this.ObserveKeys(dependencies.Keys),
                kv => consulState.UpdateKVNode(kv.ToKeyValueNode()));
            Observe(() => this.ObserveKeysRecursive(dependencies.KeyPrefixes),
                kv =>
                {
                    if (kv.Result.Response == null || !kv.Result.Response.Any())
                        consulState.MarkKeyPrefixAsMissingOrEmpty(kv.KeyPrefix);
                    else
                        consulState.UpdateKVNodes(kv.ToKeyValueNodes());
                });

            return subject;
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

        private IObservable<TObservation> LongPoll<TResponse,TObservation>(Func<ulong,Task<QueryResult<TResponse>>> poll, Func<QueryResult<TResponse>,TObservation> toObservation) where TObservation : class
        {
            ulong index = default(ulong);
            return Observable.FromAsync(async () =>
            {
                try
                {
                    var result = await poll(index);
                    index = result.LastIndex;

                    var statusCodeNumber = ((int) result.StatusCode).ToString();
                    if(statusCodeNumber.StartsWith("5"))
                    {
                        //We got a 500 error and will wait 5 seconds and try again
                        Console.WriteLine($"Got a {statusCodeNumber} response. Will retry in 5 seconds...");
                        if(_retryDelay != null)
                        {
                            await Task.Delay(_retryDelay.Value);
                        }
                        return (TObservation)null;
                    }
                    
                    return toObservation(result);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    throw;
                }
            }).Repeat().Where(o => o != null);
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