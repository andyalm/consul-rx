using System;
using System.IO;
using Spiffy.Monitoring;

namespace ConsulRx.Templating
{
    public class TemplateProcessor : IDisposable
    {
        private readonly ITemplateRenderer _renderer;
        public ConsulState ConsulState { get; private set; }
        private readonly PropertyBag _properties;
        private readonly IDisposable _subscription;
        public string TemplatePath { get; }
        public string OutputPath { get; }


        public TemplateProcessor(ITemplateRenderer renderer, IObservableConsul client, string templatePath, string outputPath, PropertyBag properties = null)
        {
            _renderer = renderer;
            TemplatePath = templatePath;
            OutputPath = outputPath;
            _properties = properties;
            
            var consulDependencies = renderer.AnalyzeDependencies(templatePath, properties);
            _subscription = client.ObserveDependencies(consulDependencies).Subscribe(consulState =>
            {
                ConsulState = consulState;
                RenderTemplate();
            });
        }

        private void RenderTemplate()
        {
            var eventContext = new EventContext("ConsulRx", "RenderTemplate");
            try
            {
                eventContext["TemplatePath"] = TemplatePath;
                using (var writer = OpenOutput())
                {
                    _renderer.Render(TemplatePath, writer, ConsulState, _properties);
                }
            }
            catch (Exception ex)
            {
                eventContext.IncludeException(ex);
            }
            finally
            {
                eventContext.Dispose();
            }
        }

        private TextWriter OpenOutput()
        {
            if (OutputPath == null || OutputPath.Equals("STDOUT"))
            {
                return Console.Out;
            }

            return new StreamWriter(File.Open(OutputPath, FileMode.Create));
        }    

        public void Dispose()
        {
            _subscription.Dispose();
        }
    }
}