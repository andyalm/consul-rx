using System;
using System.Linq;
using System.Net;
using System.Reactive.Concurrency;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Consul;
using ConsulRx.TestSupport;
using FluentAssertions;
using Xunit;

namespace ConsulRx.UnitTests
{
    public class ObservableDependenciesSpec
    {
        private readonly ConsulDependencies _consulDependencies = new ConsulDependencies();
        private readonly FakeConsulClient _consulClient = new FakeConsulClient();
        private readonly ObservableConsul _observableConsul;
        private readonly ObservationSink<ConsulState> _consulStateObservations = new ObservationSink<ConsulState>();

        public ObservableDependenciesSpec()
        {
            _observableConsul = new ObservableConsul(_consulClient);
        }
        
        [Fact]
        public async Task ConsulStateIsNotStreamedUntilAResponseHasBeenRecievedForEveryDependency()
        {
            _consulDependencies.Services.Add("myservice1");
            _consulDependencies.Services.Add("myservice2");
            _consulDependencies.Keys.Add("mykey1");
            _consulDependencies.KeyPrefixes.Add("mykeyprefix1");
            
            StartObserving();
            _consulStateObservations.Should().BeEmpty();
            
            await CompleteGetAsync("mykey1", CreateKeyObservation("mykey1"));
            _consulStateObservations.Should().BeEmpty();
            
            await CompleteServiceAsync("myservice1", CreateServiceObservation("myservice1"));
            _consulStateObservations.Should().BeEmpty();
            
            await CompleteServiceAsync("myservice2", CreateServiceObservation("myservice2"));
            _consulStateObservations.Should().BeEmpty();
            
            await CompleteListAsync("mykeyprefix1", CreateKeyRecursiveObservation("mykeyprefix1"));
            _consulStateObservations.Should().NotBeEmpty();
        }

        [Fact]
        public async Task ConsulStateIsOnlyStreamedWhenServiceResponseComesWithNewValue()
        {
            _consulDependencies.Services.Add("myservice1");
            StartObserving();
            await CompleteServiceAsync("myservice1", new QueryResult<CatalogService[]>
            {
                StatusCode = HttpStatusCode.OK,
                Response = new []
                {
                    new CatalogService
                    {
                        ServiceName = "myservice1",
                        Address = "10.0.0.1",
                        Node = "mynode1"
                    }, 
                }
            });
            _consulStateObservations.Should().HaveCount(1);

            await CompleteServiceAsync("myservice1", new QueryResult<CatalogService[]>
            {
                StatusCode = HttpStatusCode.OK,
                Response = new[]
                {
                    new CatalogService
                    {
                        ServiceName = "myservice1",
                        Address = "10.0.0.1",
                        ServiceAddress = "10.0.0.1",
                        Node = "mynode1"
                    },
                }
            });
            _consulStateObservations.Should().HaveCount(1);

            //await Task.Delay(6000); //wait for next request for the service to begin
            await CompleteServiceAsync("myservice1", new QueryResult<CatalogService[]>
            {
                StatusCode = HttpStatusCode.OK,
                Response = new[]
                {
                    new CatalogService
                    {
                        ServiceName = "myservice1",
                        Address = "10.0.0.2",
                        ServiceAddress = "10.0.0.2",
                        Node = "mynode1"
                    },
                }
            });
            _consulStateObservations.Should().HaveCount(2);
        }

        [Fact]
        public async Task ConsulStateIsOnlyStreamedWhenKeyResponseComesWithNewValue()
        {
            _consulDependencies.Keys.Add("mykey1");
            StartObserving();
            await CompleteGetAsync("mykey1", new QueryResult<KVPair>
            {
                StatusCode = HttpStatusCode.OK,
                Response = new KVPair("mykey1")
                {
                    Value = Encoding.UTF8.GetBytes("myval1")
                }
            });
            _consulStateObservations.Should().HaveCount(1);

            await CompleteGetAsync("mykey1", new QueryResult<KVPair>
            {
                StatusCode = HttpStatusCode.OK,
                Response = new KVPair("mykey1")
                {
                    Value = Encoding.UTF8.GetBytes("myval1")
                }
            });
            _consulStateObservations.Should().HaveCount(1);

            await CompleteGetAsync("mykey1", new QueryResult<KVPair>
            {
                StatusCode = HttpStatusCode.OK,
                Response = new KVPair("mykey1")
                {
                    Value = Encoding.UTF8.GetBytes("myval2")
                }
            });
            _consulStateObservations.Should().HaveCount(2);
        }

        [Fact]
        public async Task ConsulStateIsOnlyStreamedWhenListResponseComesWithNewValue()
        {
            _consulDependencies.KeyPrefixes.Add("mykey1");
            StartObserving();
            await CompleteListAsync("mykey1", new QueryResult<KVPair[]>
            {
                StatusCode = HttpStatusCode.OK,
                Response = new []
                {
                    new KVPair("mykey1/child1")
                    {
                        Value = Encoding.UTF8.GetBytes("myval1")
                    },
                    new KVPair("mykey1/child2")
                    {
                        Value = Encoding.UTF8.GetBytes("myval2")
                    }
                }
            });
            _consulStateObservations.Should().HaveCount(1);

            await CompleteListAsync("mykey1", new QueryResult<KVPair[]>
            {
                StatusCode = HttpStatusCode.OK,
                Response = new[]
                {
                    new KVPair("mykey1/child1")
                    {
                        Value = Encoding.UTF8.GetBytes("myval1")
                    },
                    new KVPair("mykey1/child2")
                    {
                        Value = Encoding.UTF8.GetBytes("myval2")
                    }
                }
            });
            _consulStateObservations.Should().HaveCount(1);

            await CompleteListAsync("mykey1", new QueryResult<KVPair[]>
            {
                StatusCode = HttpStatusCode.OK,
                Response = new[]
                {
                    new KVPair("mykey1/child1")
                    {
                        Value = Encoding.UTF8.GetBytes("myval1")
                    },
                    new KVPair("mykey1/child2")
                    {
                        Value = Encoding.UTF8.GetBytes("myval3")
                    }
                }
            });
            _consulStateObservations.Should().HaveCount(2);
        }

        [Fact]
        public async Task NotFoundErrorRetrievingServiceWillResultInEmptyServiceRecord()
        {
            _consulDependencies.Services.Add("missingservice1");
            StartObserving();
            
            await CompleteServiceAsync("missingservice1", new QueryResult<CatalogService[]>
            {
                StatusCode = HttpStatusCode.NotFound
            });
            _consulStateObservations.Should().NotBeEmpty();
            _consulStateObservations.Last().Services.Should().Contain(s => s.Name == "missingservice1");
        }

        [Fact]
        public async Task NotFoundErrorRetrievingKeyWillResultInEmptyKeyRecord()
        {
            _consulDependencies.Keys.Add("missingkey1");
            StartObserving();
            await CompleteGetAsync("missingkey1", new QueryResult<KVPair>
            {
                StatusCode = HttpStatusCode.NotFound
            });
            _consulStateObservations.Should().NotBeEmpty();
            _consulStateObservations.Last().KVStore.Should().Contain(n => n.FullKey == "missingkey1");
        }

        [Fact]
        public async Task NotFoundErrorRetrievingKeyPrefixWillStillStreamConsulState()
        {
            _consulDependencies.KeyPrefixes.Add("mykeyprefix1");
            StartObserving();
            await CompleteListAsync("mykeyprefix1", new QueryResult<KVPair[]>
            {
                StatusCode = HttpStatusCode.NotFound
            });
            _consulStateObservations.Should().NotBeEmpty();
            _consulStateObservations.Last().MissingKeyPrefixes.Should().Contain(p => p == "mykeyprefix1");
        }

        [Fact]
        public async Task ServerErrorRetrievingServiceWillBlockStreamingOfResultUntilResolved()
        {
            _consulDependencies.Services.Add("myservice1");
            StartObserving();
            await CompleteServiceAsync("myservice1", new QueryResult<CatalogService[]>
            {
                StatusCode = HttpStatusCode.InternalServerError
            });
            _consulStateObservations.Should().BeEmpty();

            //resolve error
            await CompleteServiceAsync("myservice1", CreateServiceObservation("myservice1"));
            _consulStateObservations.Should().NotBeEmpty();
            _consulStateObservations.Last().Services.Should().Contain(s => s.Name == "myservice1");
        }

        [Fact]
        public async Task ServerErrorRetrievingKeyWillBlockStreamingOfResultUntilResolved()
        {
            _consulDependencies.Keys.Add("mykey1");
            StartObserving();
            await CompleteGetAsync("mykey1", new QueryResult<KVPair>
            {
                StatusCode = HttpStatusCode.InternalServerError
            });
            _consulStateObservations.Should().BeEmpty();

            //resolve error
            await CompleteGetAsync("mykey1", CreateKeyObservation("mykey1"));
            _consulStateObservations.Should().NotBeEmpty();
            _consulStateObservations.Last().KVStore.Should().Contain(p => p.FullKey == "mykey1");
        }

        [Fact]
        public async Task ServerErrorRetrievingKeyRecursiveWillBlockStreamingOfResultUntilResolved()
        {
            _consulDependencies.KeyPrefixes.Add("mykeyprefix1");
            StartObserving();
            await CompleteListAsync("mykeyprefix1", new QueryResult<KVPair[]>
            {
                StatusCode = HttpStatusCode.InternalServerError
            });
            _consulStateObservations.Should().BeEmpty();

            //resolve error
            await CompleteListAsync("mykeyprefix1", CreateKeyRecursiveObservation("mykeyprefix1"));
            _consulStateObservations.Should().NotBeEmpty();
            _consulStateObservations.Last().KVStore.Should().Contain(k => k.FullKey.StartsWith("mykeyprefix1/"));
        }

        private Task CompleteServiceAsync(string serviceName, QueryResult<CatalogService[]> result)
        {
            return Task.WhenAll(
                _consulStateObservations.WaitForAddAsync(),
                _consulClient.CompleteServiceAsync(serviceName, result)
            );
        }

        private Task CompleteGetAsync(string key, QueryResult<KVPair> result)
        {
            return Task.WhenAll(
                _consulStateObservations.WaitForAddAsync(),
                _consulClient.CompleteGetAsync(key, result)
            );
        }

        private Task CompleteListAsync(string keyPrefix, QueryResult<KVPair[]> result)
        {
            return Task.WhenAll(
                _consulStateObservations.WaitForAddAsync(),
                _consulClient.CompleteListAsync(keyPrefix, result)
            );
        }

        private void StartObserving()
        {
            _observableConsul.ObserveDependencies(_consulDependencies).Subscribe(s => _consulStateObservations.Add(s));
        }
        
        private QueryResult<KVPair> CreateKeyObservation(string key)
        {
            return new QueryResult<KVPair>
            {
                StatusCode = HttpStatusCode.OK,
                Response = new KVPair(key)
                {
                    Value = new byte[0]
                }
            };
        }

        private QueryResult<KVPair[]> CreateKeyRecursiveObservation(string keyPrefix)
        {
            return new QueryResult<KVPair[]>
            {
                StatusCode = HttpStatusCode.OK,
                Response = new []
                {
                    new KVPair($"{keyPrefix}/child1")
                    {
                        Value = new byte[0]
                    },
                    new KVPair($"{keyPrefix}/child2")
                    {
                        Value = new byte[0]
                    }
                }
            };
        }

        private QueryResult<CatalogService[]> CreateServiceObservation(string serviceName)
        {
            return new QueryResult<CatalogService[]>
            {
                StatusCode = HttpStatusCode.OK,
                Response = new[]
                {
                    new CatalogService
                    {
                        ServiceName = serviceName,
                        Address = serviceName,
                        Node = serviceName,
                        ServiceAddress = serviceName,
                        ServicePort = 80,
                        ServiceTags = new string[0]
                    }
                }
            };
        }
    }
}