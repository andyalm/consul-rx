using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Consul;
using ConsulRx.TestSupport;
using FluentAssertions;
using FluentAssertions.Common;
using Xunit;

namespace ConsulRx.UnitTests
{
    public class ObservableConsulSpec
    {
        private readonly ObservableConsul _observableConsul;
        private readonly FakeConsulClient _consulClient;

        public ObservableConsulSpec()
        {
            _consulClient = new FakeConsulClient();
            _observableConsul = new ObservableConsul(_consulClient);
        }

        [Fact]
        public async Task OkServiceResponseIsStreamed()
        {
            List<ServiceObservation> observations = new List<ServiceObservation>();
            _observableConsul.ObserveService("MyService").Subscribe(o => {
                observations.Add(o);
            });
            await _consulClient.CompleteServiceAsync("MyService", new QueryResult<CatalogService[]>
            {
                StatusCode = HttpStatusCode.OK,
                Response = new CatalogService[] {
                    new CatalogService {
                        ServiceName = "MyService",
                        ServiceAddress = "10.8.8.3"
                    }
                }
            });
            observations.Should().HaveCount(1);
            observations[0].ServiceName.Should().Be("MyService");
            observations[0].Result.Response[0].ServiceAddress.Should().Be("10.8.8.3");
        }

        [Fact]
        public async Task ServerErrorStreamsExceptionIfEncounteredOnFirstRequest()
        {
            List<ServiceObservation> observations = new List<ServiceObservation>();
            Exception exception = null;
            _observableConsul.ObserveService("MyService").Subscribe(o => {
                observations.Add(o);
            }, ex =>
            {
                exception = ex;
            });
            await _consulClient.CompleteServiceAsync("MyService", new QueryResult<CatalogService[]>
            {
                StatusCode = HttpStatusCode.InternalServerError
            });
            observations.Should().BeEmpty();
            exception.Should().NotBeNull();
            exception.Should().BeAssignableTo<ConsulErrorException>();
            ((ConsulErrorException)exception).Result.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        }
        
        [Fact]
        public async Task ServerErrorIsRetriedIfEncounteredAfterSucceedingBefore()
        {
            List<ServiceObservation> observations = new List<ServiceObservation>();
            List<Exception> errorObservations = new List<Exception>();
            _observableConsul.ObserveService("MyService").Subscribe(
                o =>
                {
                    observations.Add(o);
                }, 
                ex => errorObservations.Add(ex));
            await _consulClient.CompleteServiceAsync("MyService", new QueryResult<CatalogService[]>
            {
                StatusCode = HttpStatusCode.OK,
                Response = new []
                {
                    new CatalogService
                    {
                        ServiceName = "MyService",
                        ServiceAddress = "10.8.8.3"
                    }
                }
            });
            await _consulClient.CompleteServiceAsync("MyService", new QueryResult<CatalogService[]>
            {
                StatusCode = HttpStatusCode.InternalServerError
            });
            await _consulClient.CompleteServiceAsync("MyService", new QueryResult<CatalogService[]>
            {
                StatusCode = HttpStatusCode.OK,
                Response = new []
                {
                    new CatalogService
                    {
                        ServiceName = "MyService",
                        ServiceAddress = "10.8.8.3"
                    }
                }
            });
            errorObservations.Should().BeEmpty();
            observations.Should().HaveCount(2);
        }

        [Fact]
        public async Task FolderKeysAreIgnoredWhenObservedViaKeysRecursive()
        {
            List<KeyRecursiveObservation> observations = new List<KeyRecursiveObservation>();
            _observableConsul.ObserveKeyRecursive("apps/myapp").Subscribe(o => {
                observations.Add(o);
            });
            await _consulClient.CompleteListAsync("apps/myapp", new QueryResult<KVPair[]>
            {
                StatusCode = HttpStatusCode.OK,
                Response = new KVPair[] {
                    new KVPair("apps/myapp/folder") { Value = null},
                    new KVPair("apps/myapp/folder/key1")
                    {
                        Value = Encoding.UTF8.GetBytes("val1")
                    }, 
                }
            });
            observations.Should().HaveCount(1);
            var kvNodes = observations[0].ToKeyValueNodes();
            kvNodes.Should().HaveCount(2);
            kvNodes.Should().ContainSingle(n => n.FullKey == "apps/myapp/folder/key1").Which.Value.Should().Be("val1");
            kvNodes.Should().ContainSingle(n => n.FullKey == "apps/myapp/folder").Which.Value.Should().BeNull();
        }
    }
}