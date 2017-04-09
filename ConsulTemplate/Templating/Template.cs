using System.IO;
using ConsulTemplate.Reactive;
using RazorLight;
using RazorLight.Compilation;

namespace ConsulTemplate.Templating
{
    public class Template
    {
        private readonly IRazorLightEngine _engine;
        private readonly string _filename;

        public Template(string templatePath)
        {
            var templateFolder = Path.GetDirectoryName(Path.GetFullPath(templatePath));
            _filename = Path.GetFileName(templatePath);
            _engine = EngineFactory.CreatePhysical(templateFolder);
        }

        public void Render(TextWriter writer, TemplateModel model)
        {
            writer.Write(_engine.Parse(_filename, model));
        }
    }
}