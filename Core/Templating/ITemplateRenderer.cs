using System.IO;
using ConsulRazor.Reactive;

namespace ConsulRazor.Templating
{
    public interface ITemplateRenderer
    {
        ConsulDependencies AnalyzeDependencies(string templatePath, PropertyBag properties = null);
        ConsulDependencies AnalyzePartialDependencies(string name, string parentTemplatePath, PropertyBag properties);

        void Render(string templatePath, TextWriter writer, ConsulState model, PropertyBag properties = null);
        void RenderPartial(string name, string parentTemplatePath, TextWriter writer, ConsulState model, PropertyBag properties);
    }
}