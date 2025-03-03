using System;
using System.IO;
using System.Linq;
using System.Reflection;
using AwesomeAssertions;
using Xunit;

namespace ConsulRx.Templating.UnitTests
{
    public class RazorTemplateCompilerSpec : IDisposable
    {
        private readonly string _tempDir;
        private readonly RazorTemplateCompiler _compiler = new RazorTemplateCompiler();

        public RazorTemplateCompilerSpec()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), $"consul-rx-tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, recursive: true);
        }

        private string CreateTemplate(string content, string fileName = "test.cshtml")
        {
            var path = Path.Combine(_tempDir, fileName);
            File.WriteAllText(path, content);
            return path;
        }

        [Fact]
        public void CompilesSimpleTemplate()
        {
            var templatePath = CreateTemplate("Hello World");
            var templates = new[] { new TemplateMetadata(templatePath) };

            var assembly = _compiler.Compile(templates, typeof(ConsulTemplateBase));

            assembly.Should().NotBeNull();
        }

        [Fact]
        public void CompiledTemplateInheritsBaseClass()
        {
            var templatePath = CreateTemplate("Hello World");
            var metadata = new TemplateMetadata(templatePath);
            var templates = new[] { metadata };

            var assembly = _compiler.Compile(templates, typeof(ConsulTemplateBase));
            var templateType = assembly.GetType(metadata.FullTypeName);

            templateType.Should().NotBeNull();
            typeof(ConsulTemplateBase).IsAssignableFrom(templateType).Should().BeTrue();
        }

        [Fact]
        public void CompiledTemplateRendersStaticContent()
        {
            var templatePath = CreateTemplate("Hello World");
            var metadata = new TemplateMetadata(templatePath);
            var templates = new[] { metadata };

            var assembly = _compiler.Compile(templates, typeof(ConsulTemplateBase));
            var instance = (ConsulTemplateBase)Activator.CreateInstance(assembly.GetType(metadata.FullTypeName));

            var writer = new StringWriter();
            instance.Render(writer, new ConsulState(), null, null);

            writer.ToString().Should().Be("Hello World");
        }

        [Fact]
        public void CompiledTemplateRendersExpression()
        {
            var templatePath = CreateTemplate("@(1 + 2)");
            var metadata = new TemplateMetadata(templatePath);
            var templates = new[] { metadata };

            var assembly = _compiler.Compile(templates, typeof(ConsulTemplateBase));
            var instance = (ConsulTemplateBase)Activator.CreateInstance(assembly.GetType(metadata.FullTypeName));

            var writer = new StringWriter();
            instance.Render(writer, new ConsulState(), null, null);

            writer.ToString().Should().Be("3");
        }

        [Fact]
        public void CompilesMultipleTemplates()
        {
            var path1 = CreateTemplate("First", "first.cshtml");
            var path2 = CreateTemplate("Second", "second.cshtml");
            var metadata1 = new TemplateMetadata(path1);
            var metadata2 = new TemplateMetadata(path2);

            var assembly = _compiler.Compile(new[] { metadata1, metadata2 }, typeof(ConsulTemplateBase));

            assembly.GetType(metadata1.FullTypeName).Should().NotBeNull();
            assembly.GetType(metadata2.FullTypeName).Should().NotBeNull();
        }

        [Fact]
        public void ThrowsOnInvalidTemplate()
        {
            var templatePath = CreateTemplate("@{ invalid csharp syntax !@#$ }");
            var templates = new[] { new TemplateMetadata(templatePath) };

            Action act = () => _compiler.Compile(templates, typeof(ConsulTemplateBase));

            act.Should().Throw<TemplateCompilationException>();
        }
    }
}
