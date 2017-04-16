using System.IO;
using ConsulTemplate.Reactive;

namespace ConsulTemplate.Templating
{
    public interface ITemplateRenderer
    {
        TemplateDependencies AnalyzeDependencies(string templatePath);
        void Render(string templatePath, TextWriter writer, ConsulState model);
    }
}