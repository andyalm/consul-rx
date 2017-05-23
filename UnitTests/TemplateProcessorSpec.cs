using System.IO;
using System.Net;
using Consul;
using ReactiveConsul;
using ConsulRazor.Templating;
using FluentAssertions;
using Moq;
using ReactiveConsul.TestSupport;
using Xunit;

namespace ConsulRazor.UnitTests
{
    public class TemplateProcessorSpec
    {
        private readonly ConsulDependencies _consulDependencies = new ConsulDependencies();
        private readonly Mock<ITemplateRenderer> _renderer = new Mock<ITemplateRenderer>();
        private readonly FakeObservableConsul _consul = new FakeObservableConsul();

        public TemplateProcessorSpec()
        {
            _renderer.Setup(r => r.AnalyzeDependencies(It.IsAny<string>(), It.IsAny<PropertyBag>())).Returns(_consulDependencies);
        }

        [Fact]
        public void ServicesAreObserved()
        {
            _consulDependencies.Services.Add("myservice1");
            CreateProcessor();
            _consul.ObservingServices.Should().HaveCount(1);
            _consul.ObservingServices.Should().Contain("myservice1");
        }
        
        [Fact]
        public void KeysAreObserved()
        {
            _consulDependencies.Keys.Add("mykey1");
            CreateProcessor();
            _consul.ObservingKeys.Should().HaveCount(1);
            _consul.ObservingKeys.Should().Contain("mykey1");
        }

        [Fact]
        public void KeysRecursiveAreObserved()
        {
            _consulDependencies.KeyPrefixes.Add("mykey1");
            CreateProcessor();
            _consul.ObservingKeyPrefixes.Should().HaveCount(1);
            _consul.ObservingKeyPrefixes.Should().Contain("mykey1");
        }

        [Fact]
        public void TemplateIsNotRenderedUntilAllDependenciesHaveResponded()
        {
            _consulDependencies.Services.Add("myservice1");
            _consulDependencies.Services.Add("myservice2");
            _consulDependencies.Keys.Add("mykey1");
            _consulDependencies.KeyPrefixes.Add("mykeyprefix1");
            CreateProcessor();
            VerifyRenderIsCalled(Times.Never());
            _consul.Keys.OnNext(CreateKeyObservation("mykey1"));
            VerifyRenderIsCalled(Times.Never());
            _consul.Services.OnNext(CreateServiceObservation("myservice1"));
            VerifyRenderIsCalled(Times.Never());
            _consul.Services.OnNext(CreateServiceObservation("myservice2"));
            VerifyRenderIsCalled(Times.Never());
            _consul.KeysRecursive.OnNext(CreateKeyRecursiveObservation("mykeyprefix1"));
            VerifyRenderIsCalled(Times.Once());
        }

        [Fact]
        public void NotFoundErrorRetrievingServiceWillResultInEmptyServiceRecord()
        {
            _consulDependencies.Services.Add("missingservice1");
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
            _consulDependencies.Keys.Add("missingkey1");
            var processor = CreateProcessor();
            _consul.Keys.OnNext(new KeyObservation("missingkey1", new QueryResult<KVPair>
            {
                StatusCode = HttpStatusCode.NotFound
            }));
            VerifyRenderIsCalled(Times.Once());
            processor.ConsulState.KVStore.Should().Contain(n => n.FullKey == "missingkey1");
        }

        [Fact]
        public void NotFoundErrorRetrievingKeyPrefixWillStillAllowTemplateToBeRendered()
        {
            _consulDependencies.KeyPrefixes.Add("mykeyprefix1");
            var processor = CreateProcessor();
            _consul.KeysRecursive.OnNext(new KeyRecursiveObservation("mykeyprefix1", new QueryResult<KVPair[]>
            {
                StatusCode = HttpStatusCode.NotFound
            }));
            VerifyRenderIsCalled(Times.Once());
            processor.ConsulState.MissingKeyPrefixes.Should().Contain(p => p == "mykeyprefix1");
        }

        [Fact]
        public void ServerErrorRetrievingServiceWillBlockTemplateRenderingUntilResolved()
        {
            _consulDependencies.Services.Add("myservice1");
            var processor = CreateProcessor();
            _consul.Services.OnNext(new ServiceObservation("myservice1", new QueryResult<CatalogService[]>
            {
                StatusCode = HttpStatusCode.InternalServerError
            }));
            VerifyRenderIsCalled(Times.Never());

            //resolve error
            _consul.Services.OnNext(CreateServiceObservation("myservice1"));
            VerifyRenderIsCalled(Times.Once());
            processor.ConsulState.Services.Should().Contain(s => s.Name == "myservice1");
        }

        [Fact]
        public void ServerErrorRetrievingKeyWillBlockTemplateRenderingUntilResolved()
        {
            _consulDependencies.Keys.Add("mykey1");
            var processor = CreateProcessor();
            _consul.Keys.OnNext(new KeyObservation("mykey1", new QueryResult<KVPair>
            {
                StatusCode = HttpStatusCode.InternalServerError
            }));
            VerifyRenderIsCalled(Times.Never());

            //resolve error
            _consul.Keys.OnNext(CreateKeyObservation("mykey1"));
            VerifyRenderIsCalled(Times.Once());
            processor.ConsulState.KVStore.Should().Contain(p => p.FullKey == "mykey1");
        }

        [Fact]
        public void ServerErrorRetrievingKeyRecursiveWillBlockTemplateRenderingUntilResolved()
        {
            _consulDependencies.KeyPrefixes.Add("mykeyprefix1");
            var processor = CreateProcessor();
            _consul.KeysRecursive.OnNext(new KeyRecursiveObservation("mykeyprefix1", new QueryResult<KVPair[]>
            {
                StatusCode = HttpStatusCode.InternalServerError
            }));
            VerifyRenderIsCalled(Times.Never());

            //resolve error
            _consul.KeysRecursive.OnNext(CreateKeyRecursiveObservation("mykeyprefix1"));
            VerifyRenderIsCalled(Times.Once());
            processor.ConsulState.KVStore.Should().Contain(k => k.FullKey.StartsWith("mykeyprefix1/"));
        }

        private void VerifyRenderIsCalled(Times times)
        {
            _renderer.Verify(r => r.Render(It.IsAny<string>(), It.IsAny<TextWriter>(), It.IsAny<ConsulState>(), It.IsAny<PropertyBag>()), times);
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

        private KeyRecursiveObservation CreateKeyRecursiveObservation(string keyPrefix)
        {
            return new KeyRecursiveObservation(keyPrefix, new QueryResult<KVPair[]>
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

        private TemplateProcessor CreateProcessor() => new TemplateProcessor(_renderer.Object, _consul, "mytemplate.razor", null, new PropertyBag());
    }
}
