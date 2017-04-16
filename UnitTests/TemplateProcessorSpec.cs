using System;
using System.IO;
using System.Linq;
using System.Net;
using Consul;
using ConsulTemplate.Reactive;
using ConsulTemplate.Templating;
using ConsulTemplate.UnitTests.Support;
using FluentAssertions;
using Moq;
using Xunit;

namespace ConsulTemplate.UnitTests
{
    public class TemplateProcessorSpec
    {
        private readonly TemplateDependencies _templateDependencies = new TemplateDependencies();
        private readonly Mock<ITemplateRenderer> _renderer = new Mock<ITemplateRenderer>();
        private readonly FakeObservableConsul _consul = new FakeObservableConsul();

        public TemplateProcessorSpec()
        {
            _renderer.Setup(r => r.AnalyzeDependencies(It.IsAny<string>())).Returns(_templateDependencies);
        }

        [Fact]
        public void RequiredServicesAreObserved()
        {
            _templateDependencies.Services.Add("myservice1");
            CreateProcessor();
            _consul.ObservingServices.Should().HaveCount(1);
            _consul.ObservingServices.Should().Contain("myservice1");
        }
        
        [Fact]
        public void RequiredKeysAreObserved()
        {
            _templateDependencies.Keys.Add("mykey1");
            CreateProcessor();
            _consul.ObservingKeys.Should().HaveCount(1);
            _consul.ObservingKeys.Should().Contain("mykey1");
        }

        [Fact]
        public void KeyPrefixesAreObserved()
        {
            _templateDependencies.KeyPrefixes.Add("mykey1");
            CreateProcessor();
            _consul.ObservingKeyPrefixes.Should().HaveCount(1);
            _consul.ObservingKeyPrefixes.Should().Contain("mykey1");
        }

        [Fact]
        public void TemplateIsNotRenderedUntilAllDependenciesHaveResponded()
        {
            _templateDependencies.Services.Add("myservice1");
            _templateDependencies.Services.Add("myservice2");
            _templateDependencies.Keys.Add("mykey1");
            CreateProcessor();
            VerifyRenderIsCalled(Times.Never());
            _consul.Keys.OnNext(CreateKeyObservation("mykey1"));
            VerifyRenderIsCalled(Times.Never());
            _consul.Services.OnNext(CreateServiceObservation("myservice1"));
            VerifyRenderIsCalled(Times.Never());
            _consul.Services.OnNext(CreateServiceObservation("myservice2"));
            VerifyRenderIsCalled(Times.Once());
        }

        [Fact]
        public void NotFoundErrorRetrievingServiceWillResultInEmptyServiceRecord()
        {
            _templateDependencies.Services.Add("missingservice1");
            var processor = CreateProcessor();
            _consul.Services.OnNext(new ServiceObservation("missingservice1", new QueryResult<CatalogService[]>
            {
                StatusCode = HttpStatusCode.NotFound
            }));
            VerifyRenderIsCalled(Times.Once());
            processor.ConsulState.Services.Should().Contain(s => s.Name == "missingservice1");
        }

        [Fact]
        public void NotFoundErrorRetrievingKeyWillResultInEmptyKeyRecordAndAllowTemplateToRender()
        {
            _templateDependencies.Keys.Add("missingkey1");
            var processor = CreateProcessor();
            _consul.Keys.OnNext(new KeyObservation("missingkey1", new QueryResult<KVPair>
            {
                StatusCode = HttpStatusCode.NotFound
            }));
            VerifyRenderIsCalled(Times.Once());
            processor.ConsulState.KVStore.Should().Contain(n => n.FullKey == "missingkey1");
        }

        [Fact]
        public void ServerErrorRetrievingServiceWillBlockTemplateRenderingUntilResolved()
        {
            _templateDependencies.Services.Add("myservice1");
            var processor = CreateProcessor();
            _consul.Services.OnNext(new ServiceObservation("myservice1", new QueryResult<CatalogService[]>
            {
                StatusCode = HttpStatusCode.InternalServerError
            }));
            VerifyRenderIsCalled(Times.Never());
            processor.ConsulState.Services.Should().NotContain(s => s.Name == "myservice1");

            //resolve error
            _consul.Services.OnNext(CreateServiceObservation("myservice1"));
            VerifyRenderIsCalled(Times.Once());
            processor.ConsulState.Services.Should().Contain(s => s.Name == "myservice1");
        }

        [Fact]
        public void ServerErrorRetrievingKeyWillBlockTemplateRenderingUntilResolved()
        {
            _templateDependencies.Keys.Add("mykey1");
            var processor = CreateProcessor();
            _consul.Keys.OnNext(new KeyObservation("mykey1", new QueryResult<KVPair>
            {
                StatusCode = HttpStatusCode.InternalServerError
            }));
            VerifyRenderIsCalled(Times.Never());
            processor.ConsulState.KVStore.Should().NotContain(s => s.FullKey == "mykey1");

            //resolve error
            _consul.Keys.OnNext(CreateKeyObservation("mykey1"));
            VerifyRenderIsCalled(Times.Once());
            processor.ConsulState.KVStore.Should().Contain(p => p.FullKey == "mykey1");
        }

        private void VerifyRenderIsCalled(Times times)
        {
            _renderer.Verify(r => r.Render(It.IsAny<string>(), It.IsAny<TextWriter>(), It.IsAny<ConsulState>()), times);
        }

        private KeyObservation CreateKeyObservation(string key)
        {
            return new KeyObservation(key, new QueryResult<KVPair>
            {
                StatusCode = HttpStatusCode.OK,
                Response = new KVPair(key)
                {
                    Value = new byte[0]
                }
            });
        }

        private ServiceObservation CreateServiceObservation(string serviceName)
        {
            return new ServiceObservation(serviceName, new QueryResult<CatalogService[]>
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
            });
        }

        private TemplateProcessor CreateProcessor() => new TemplateProcessor(_renderer.Object, _consul, "mytemplate.razor");
    }
}
