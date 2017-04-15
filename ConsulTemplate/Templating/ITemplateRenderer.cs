using System.IO;
using ConsulTemplate.Reactive;

namespace ConsulTemplate.Templating
{
    public interface ITemplateRenderer
    {
        TemplateAnalysis Analyse(string templatePath);
        void Render(string templatePath, TextWriter writer, ConsulState model);
    }
}