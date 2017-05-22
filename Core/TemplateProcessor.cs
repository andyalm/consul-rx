using System;
using System.IO;
using ConsulRazor.Reactive;
using ConsulRazor.Templating;

namespace ConsulRazor
{
    public class TemplateProcessor : IDisposable
    {
        private readonly ITemplateRenderer _renderer;
        public ConsulState ConsulState { get; private set; }
        private readonly PropertyBag _properties;
        public string TemplatePath { get; }
        public string OutputPath { get; }


        public TemplateProcessor(ITemplateRenderer renderer, IObservableConsul client, string templatePath, string outputPath, PropertyBag properties = null)
        {
            _renderer = renderer;
            TemplatePath = templatePath;
            OutputPath = outputPath;
            _properties = properties;
            
            var consulDependencies = renderer.AnalyzeDependencies(templatePath, properties);
            client.ObserveDependencies(consulDependencies).ContinueWith(consulState =>
            {
                ConsulState = consulState.Result;
                RenderTemplate();
                ConsulState.Changes.Subscribe(_ => RenderTemplate());
            });
        }

        private void RenderTemplate()
        {
            try
            {
                Console.WriteLine($"Rendering template '{TemplatePath}'");
                using (var writer = OpenOutput())
                {
                    _renderer.Render(TemplatePath, writer, ConsulState, _properties);
                }
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
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
            ConsulState?.Dispose();
        }
    }
}