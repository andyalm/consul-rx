using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
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
        private readonly List<ConsulState> _consulStateObservations = new List<ConsulState>();

        public ObservableDependenciesSpec()
        {
            _observableConsul = new ObservableConsul(_consulClient);
            
        }
        
        [Fact]
        public void ConsulStateIsNotStreamedUntilAResponseHasBeenRecievedForEveryDependency()
        {
            _consulDependencies.Services.Add("myservice1");
            _consulDependencies.Services.Add("myservice2");
            _consulDependencies.Keys.Add("mykey1");
            _consulDependencies.KeyPrefixes.Add("mykeyprefix1");
            
            StartObserving();
            _consulStateObservations.Should().BeEmpty();
            
            CompleteGet("mykey1", CreateKeyObservation("mykey1"));
            _consulStateObservations.Should().BeEmpty();
            
            CompleteService("myservice1", CreateServiceObservation("myservice1"));
            _consulStateObservations.Should().BeEmpty();
            
            CompleteService("myservice2", CreateServiceObservation("myservice2"));
            _consulStateObservations.Should().BeEmpty();
            
            CompleteList("mykeyprefix1", CreateKeyRecursiveObservation("mykeyprefix1"));
            _consulStateObservations.Should().NotBeEmpty();
        }

        [Fact]
        public void NotFoundErrorRetrievingServiceWillResultInEmptyServiceRecord()
        {
            _consulDependencies.Services.Add("missingservice1");
            StartObserving();
            
            CompleteService("missingservice1", new QueryResult<CatalogService[]>
            {
                StatusCode = HttpStatusCode.NotFound
            });
            _consulStateObservations.Should().NotBeEmpty();
            _consulStateObservations.Last().Services.Should().Contain(s => s.Name == "missingservice1");
        }

        [Fact]
        public void NotFoundErrorRetrievingKeyWillResultInEmptyKeyRecord()
        {
            _consulDependencies.Keys.Add("missingkey1");
            StartObserving();
            CompleteGet("missingkey1", new QueryResult<KVPair>
            {
                StatusCode = HttpStatusCode.NotFound
            });
            _consulStateObservations.Should().NotBeEmpty();
            _consulStateObservations.Last().KVStore.Should().Contain(n => n.FullKey == "missingkey1");
        }

        [Fact]
        public void NotFoundErrorRetrievingKeyPrefixWillStillStreamConsulState()
        {
            _consulDependencies.KeyPrefixes.Add("mykeyprefix1");
            StartObserving();
            CompleteList("mykeyprefix1", new QueryResult<KVPair[]>
            {
                StatusCode = HttpStatusCode.NotFound
            });
            _consulStateObservations.Should().NotBeEmpty();
            _consulStateObservations.Last().MissingKeyPrefixes.Should().Contain(p => p == "mykeyprefix1");
        }

        [Fact]
        public void ServerErrorRetrievingServiceWillBlockStreamingOfResultUntilResolved()
        {
            _consulDependencies.Services.Add("myservice1");
            StartObserving();
            CompleteService("myservice1", new QueryResult<CatalogService[]>
            {
                StatusCode = HttpStatusCode.InternalServerError
            });
            _consulStateObservations.Should().BeEmpty();

            //resolve error
            CompleteService("myservice1", CreateServiceObservation("myservice1"));
            _consulStateObservations.Should().NotBeEmpty();
            _consulStateObservations.Last().Services.Should().Contain(s => s.Name == "myservice1");
        }

        [Fact]
        public void ServerErrorRetrievingKeyWillBlockStreamingOfResultUntilResolved()
        {
            _consulDependencies.Keys.Add("mykey1");
            StartObserving();
            CompleteGet("mykey1", new QueryResult<KVPair>
            {
                StatusCode = HttpStatusCode.InternalServerError
            });
            _consulStateObservations.Should().BeEmpty();

            //resolve error
            CompleteGet("mykey1", CreateKeyObservation("mykey1"));
            _consulStateObservations.Should().NotBeEmpty();
            _consulStateObservations.Last().KVStore.Should().Contain(p => p.FullKey == "mykey1");
        }

        [Fact]
        public void ServerErrorRetrievingKeyRecursiveWillBlockStreamingOfResultUntilResolved()
        {
            _consulDependencies.KeyPrefixes.Add("mykeyprefix1");
            StartObserving();
            CompleteList("mykeyprefix1", new QueryResult<KVPair[]>
            {
                StatusCode = HttpStatusCode.InternalServerError
            });
            _consulStateObservations.Should().BeEmpty();

            //resolve error
            CompleteList("mykeyprefix1", CreateKeyRecursiveObservation("mykeyprefix1"));
            _consulStateObservations.Should().NotBeEmpty();
            _consulStateObservations.Last().KVStore.Should().Contain(k => k.FullKey.StartsWith("mykeyprefix1/"));
        }

        private void CompleteService(string serviceName, QueryResult<CatalogService[]> result)
        {
            _consulClient.CompleteService(serviceName, result);
            Thread.Sleep(10);
        }

        private void CompleteGet(string key, QueryResult<KVPair> result)
        {
            _consulClient.CompleteGet(key, result);
            Thread.Sleep(10);
        }

        private void CompleteList(string keyPrefix, QueryResult<KVPair[]> result)
        {
            _consulClient.CompleteList(keyPrefix, result);
            Thread.Sleep(10);
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