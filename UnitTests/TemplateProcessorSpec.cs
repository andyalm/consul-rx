using System;
using ConsulTemplate.Templating;
using ConsulTemplate.UnitTests.Support;
using FluentAssertions;
using Moq;
using Xunit;

namespace ConsulTemplate.UnitTests
{
    public class TemplateProcessorSpec
    {
        private readonly TemplateAnalysis _templateAnalysis = new TemplateAnalysis();
        private readonly Mock<ITemplateRenderer> _renderer = new Mock<ITemplateRenderer>();
        private readonly FakeObservableConsul _consul = new FakeObservableConsul();

        public TemplateProcessorSpec()
        {
            _renderer.Setup(r => r.Analyse(It.IsAny<string>())).Returns(_templateAnalysis);
        }

        [Fact]
        public void RequiredServicesAreObserved()
        {
            _templateAnalysis.RequiredServices.Add("myservice1");
            CreateProcessor();
            _consul.ObservingServices.Should().HaveCount(1);
            _consul.ObservingServices.Should().Contain("myservice1");
        }
        
        [Fact]
        public void RequiredKeysAreObserved()
        {
            _templateAnalysis.RequiredKeys.Add("mykey1");
            CreateProcessor();
            _consul.ObservingKeys.Should().HaveCount(1);
            _consul.ObservingKeys.Should().Contain("mykey1");
        }

        [Fact]
        public void OptionalKeysAreObserved()
        {
            _templateAnalysis.OptionalKeys.Add("mykey1");
            CreateProcessor();
            _consul.ObservingKeys.Should().HaveCount(1);
            _consul.ObservingKeys.Should().Contain("mykey1");
        }

        [Fact]
        public void KeyPrefixesAreObserved()
        {
            _templateAnalysis.KeyPrefixes.Add("mykey1");
            CreateProcessor();
            _consul.ObservingKeyPrefixes.Should().HaveCount(1);
            _consul.ObservingKeyPrefixes.Should().Contain("mykey1");
        }

        private TemplateProcessor CreateProcessor() => new TemplateProcessor(_renderer.Object, _consul, "mytemplate.razor");
    }
}
