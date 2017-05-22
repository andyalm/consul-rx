using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System;
using System.Collections.Generic;
using Consul;
using ConsulRazor.Reactive;
using ConsulRazor.UnitTests.Support;
using FluentAssertions;
using Moq;
using Xunit;
using System.Threading;

namespace ConsulRazor.UnitTests
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
        public void ServerErrorDoesNotStreamItem()
        {
            List<ServiceObservation> observations = new List<ServiceObservation>();
            AutoResetEvent observationSignal = new AutoResetEvent(false);
            _observableConsul.ObserveService("MyService").Subscribe(o => {
                observations.Add(o);
                observationSignal.Set();
            });
            _consulClient.CompleteService("MyService", new QueryResult<CatalogService[]>
            {
                StatusCode = HttpStatusCode.InternalServerError
            });
            observationSignal.WaitOne(100);
            observations.Should().BeEmpty();
        }

        [Fact]
        public void OkServiceResponseIsStreamed()
        {
            List<ServiceObservation> observations = new List<ServiceObservation>();
            AutoResetEvent observationSignal = new AutoResetEvent(false);
            _observableConsul.ObserveService("MyService").Subscribe(o => {
                observations.Add(o);
                observationSignal.Set();
            });
            _consulClient.CompleteService("MyService", new QueryResult<CatalogService[]>
            {
                StatusCode = HttpStatusCode.OK,
                Response = new CatalogService[] {
                    new CatalogService {
                        ServiceName = "MyService",
                        ServiceAddress = "10.8.8.3"
                    }
                }
            });
            observationSignal.WaitOne(100);
            observations.Should().HaveCount(1);
            observations[0].ServiceName.Should().Be("MyService");
            observations[0].Result.Response[0].ServiceAddress.Should().Be("10.8.8.3");
        }
    }
}