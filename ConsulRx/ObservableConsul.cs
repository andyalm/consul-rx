using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Consul;
using Spiffy.Monitoring;

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
                if(!string.IsNullOrEmpty(config.Endpoint))
                    c.Address = new Uri(config.Endpoint);
                if(!string.IsNullOrEmpty(config.Datacenter))
                    c.Datacenter = config.Datacenter;
            });
            _longPollMaxWait = config.LongPollMaxWait;
            _retryDelay = config.RetryDelay;
            _aclToken = config.AclToken;
        }

        public IObservable<ServiceObservation> ObserveService(string serviceName)
        {
            return LongPoll(index => _client.Catalog.Service(serviceName, null,
                QueryOptions(index)), result => new ServiceObservation(serviceName, result),
                "GetService", new Dictionary<string, object>
                {
                    {"ServiceName",serviceName}
                });
        }

        public IObservable<KeyObservation> ObserveKey(string key)
        {
            return LongPoll(index => _client.KV.Get(key, QueryOptions(index)), result => new KeyObservation(key, result),
                "GetKey", new Dictionary<string, object>
                {
                    {"Key", key}
                });
        }

        public IObservable<KeyRecursiveObservation> ObserveKeyRecursive(string prefix)
        {
            return LongPoll(index => _client.KV.List(prefix, QueryOptions(index)), result => new KeyRecursiveObservation(prefix, result),
                "GetKeys", new Dictionary<string, object>
                {
                    {"KeyPrefix", prefix}
                });
        }

        public IObservable<ConsulState> ObserveDependencies(ConsulDependencies dependencies)
        {
            var consulState = new ConsulState();
            var updateMutex = new object();
            
            void WrapUpdate(string operationName, Action<EventContext> tryUpdate)
            {
                var eventContext = new EventContext("ConsulRx.ConsulState", operationName);
                try
                {
                    lock (updateMutex)
                    {
                        tryUpdate(eventContext);
                    }
                }
                catch (Exception ex)
                {
                    eventContext.IncludeException(ex);
                    throw;
                }
                finally
                {
                    eventContext.Dispose();
                }
            }
            
            var consulStateObservable = Observable.Create<ConsulState>(o =>
            {
                var compositeDisposable = new CompositeDisposable
                {
                    this.ObserveServices(dependencies.Services)
                        .Select(services => services.ToService())
                        .Subscribe(service =>
                        {
                            WrapUpdate("UpdateService", eventContext =>
                            {
                                eventContext["ServiceName"] = service.Name;
                                bool alreadyExisted = consulState.ContainsService(service.Name);
                                if (consulState.TryUpdateService(service, out var updatedState))
                                {
                                    eventContext["UpdateType"] = alreadyExisted ? "Update" : "Add";
                                    consulState = updatedState;
                                    o.OnNext(consulState);
                                }
                                else
                                {
                                    eventContext["UpdateType"] = "Noop";
                                }
                            });
                        }, o.OnError),
                    this.ObserveKeys(dependencies.Keys)
                        .Select(kv => kv.ToKeyValueNode())
                        .Subscribe(kvNode =>
                        {
                            WrapUpdate("UpdateKey", eventContext =>
                            {
                                eventContext["Key"] = kvNode.FullKey;
                                eventContext["Value"] = kvNode.Value;
                                bool alreadyExisted = consulState.ContainsKey(kvNode.FullKey);
                                if (consulState.TryUpdateKVNode(kvNode, out var updatedState))
                                {
                                    eventContext["UpdateType"] = alreadyExisted ? "Update" : "Add";
                                    consulState = updatedState;
                                    o.OnNext(consulState);
                                }
                                else
                                {
                                    eventContext["UpdateType"] = "Noop";
                                }
                            });
                        }, o.OnError),
                    this.ObserveKeysRecursive(dependencies.KeyPrefixes)
                        .Subscribe(kv =>
                        {
                            WrapUpdate("UpdateKeys", eventContext =>
                            {
                                eventContext["KeyPrefix"] = kv.KeyPrefix;
                                eventContext["ChildKeyCount"] = kv.Result.Response?.Length ?? 0;
                                if (kv.Result.Response == null || !kv.Result.Response.Any())
                                {
                                    if (consulState.TryMarkKeyPrefixAsMissingOrEmpty(kv.KeyPrefix, out var updatedState)
                                    )
                                    {
                                        eventContext["UpdateType"] = "MarkAsMissing";
                                        consulState = updatedState;
                                        o.OnNext(consulState);
                                    }
                                    else
                                    {
                                        eventContext["UpdateType"] = "Noop";
                                    }
                                }
                                else
                                {
                                    var kvNodes = kv.ToKeyValueNodes();
                                    bool alreadyExisted = consulState.ContainsKeyStartingWith(kv.KeyPrefix);
                                    if (consulState.TryUpdateKVNodes(kvNodes, out var updatedState))
                                    {
                                        eventContext["UpdateType"] = alreadyExisted ? "Update" : "Add";
                                        consulState = updatedState;
                                        o.OnNext(consulState);
                                    }
                                    else
                                    {
                                        eventContext["UpdateType"] = "Noop";
                                    }
                                }
                            });
                        }, o.OnError)
                };

                return compositeDisposable;
            });


            return consulStateObservable.Where(s => s.SatisfiesAll(dependencies));
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
        
        private static readonly HashSet<HttpStatusCode> HealthyCodes = new HashSet<HttpStatusCode>{HttpStatusCode.OK, HttpStatusCode.NotFound};

        private IObservable<TObservation> LongPoll<TResponse,TObservation>(Func<ulong,Task<QueryResult<TResponse>>> poll, Func<QueryResult<TResponse>,TObservation> toObservation, string monitoringOperation, IDictionary<string,object> monitoringProperties) where TObservation : class
        {
            return Observable.Create<TObservation>(async (o, cancel) =>
            {
                ulong index = default(ulong);
                var successfullyContactedConsulAtLeastOnce = false;
                while (true)
                {
                    using (var eventContext = new EventContext("ConsulRx.Client", monitoringOperation))
                    {
                        eventContext["RequestIndex"] = index;
                        eventContext.AddValues(monitoringProperties);
                        QueryResult<TResponse> result = null;
                        Exception exception = null;
                        try
                        {
                            result = await poll(index);
                        }
                        catch (Exception ex)
                        {
                            eventContext.IncludeException(ex);
                            exception = ex;
                        }
                        if (cancel.IsCancellationRequested)
                        {
                            eventContext["Cancelled"] = true;
                            o.OnCompleted();
                            return;
                        }
                        if (result != null)
                        {
                            index = result.LastIndex;
                            eventContext.IncludeConsulResult(result);
                            if (HealthyCodes.Contains(result.StatusCode))
                            {
                                //200 or 404 are the only response codes we should expect if consul and client are both configured properly
                                successfullyContactedConsulAtLeastOnce = true;
                            }
                            else
                            {
                                //if we got an error that indicates either server or client aren't healthy (e.g. 500 or 403)
                                //then model this as an exception (same as if server can't be contacted). We will figure out what to do below
                                exception = new ConsulErrorException(result);
                                eventContext.SetLevel(Level.Error);
                            }
                        }

                        if (exception == null)
                        {
                            o.OnNext(toObservation(result));
                        }
                        else
                        {
                            if (successfullyContactedConsulAtLeastOnce)
                            {
                                //if we have been successful at contacting consul already, then we will retry under the assumption that
                                //things will eventually get healthy again
                                eventContext["SecondsUntilRetry"] = _retryDelay?.Seconds ?? 0;
                                if (_retryDelay != null)
                                {
                                    await Task.Delay(_retryDelay.Value, cancel);
                                }
                            }
                            else
                            {
                                //if we encountered an error at the very beginning, then we don't have enough confidence that retrying will actually help
                                //so we will stream the exception out and let the consumer figure out what to do
                                o.OnError(exception);
                                return;
                            }
                        }
                    }
                }
            });
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