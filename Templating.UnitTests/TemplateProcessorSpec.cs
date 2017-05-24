using System.IO;
using ConsulRx.TestSupport;
using FluentAssertions;
using Moq;
using Xunit;

namespace ConsulRx.Templating.UnitTests
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
        public void AnalyzedDependenciesAreObserved()
        {
            _consulDependencies.Services.Add("myservice1");
            _consulDependencies.Keys.Add("mykey1");
            _consulDependencies.KeyPrefixes.Add("mykeyprefix1");
            
            CreateProcessor();
            _consul.ObservingDependencies.Should().Contain(d => d.Services.Contains("myservice1"));
            _consul.ObservingDependencies.Should().Contain(d => d.Keys.Contains("mykey1"));
            _consul.ObservingDependencies.Should().Contain(d => d.KeyPrefixes.Contains("mykeyprefix1"));
        }

        [Fact]
        public void TemplateIsNotRenderedUntilDependenciesHaveResponded()
        {
            _consulDependencies.Services.Add("myservice1");
            CreateProcessor();
            VerifyRenderIsCalled(Times.Never());
            _consul.Dependencies.OnNext(new ConsulState());
            VerifyRenderIsCalled(Times.Once());
        }

        private void VerifyRenderIsCalled(Times times)
        {
            _renderer.Verify(r => r.Render(It.IsAny<string>(), It.IsAny<TextWriter>(), It.IsAny<ConsulState>(), It.IsAny<PropertyBag>()), times);
        }

        private TemplateProcessor CreateProcessor() => new TemplateProcessor(_renderer.Object, _consul, "mytemplate.razor", null, new PropertyBag());
    }
}
