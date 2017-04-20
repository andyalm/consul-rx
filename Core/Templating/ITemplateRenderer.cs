using System.IO;
using ConsulRazor.Reactive;

namespace ConsulRazor.Templating
{
    public interface ITemplateRenderer
    {
        TemplateDependencies AnalyzeDependencies(string templatePath);
        void Render(string templatePath, TextWriter writer, ConsulState model);
    }
}