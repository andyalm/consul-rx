using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Consul;
using ConsulRx.TestSupport;
using FluentAssertions;
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
        public async Task ServerErrorDoesNotStreamItem()
        {
            List<ServiceObservation> observations = new List<ServiceObservation>();
            _observableConsul.ObserveService("MyService").Subscribe(o => {
                observations.Add(o);
            });
            await _consulClient.CompleteServiceAsync("MyService", new QueryResult<CatalogService[]>
            {
                StatusCode = HttpStatusCode.InternalServerError
            });
            observations.Should().BeEmpty();
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
    }
}