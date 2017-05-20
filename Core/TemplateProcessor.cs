using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using ConsulRazor.Reactive;
using ConsulRazor.Templating;

namespace ConsulRazor
{
    public class TemplateProcessor : IDisposable
    {
        private readonly ITemplateRenderer _renderer;
        private readonly ConsulDependencies _consulDependencies;
        public ConsulState ConsulState { get; }
        private readonly IDisposable _subscriptions;
        private readonly PropertyBag _properties;
        public string TemplatePath { get; }
        public string OutputPath { get; }


        public TemplateProcessor(ITemplateRenderer renderer, IObservableConsul client, string templatePath, string outputPath, PropertyBag properties = null)
        {
            _renderer = renderer;
            _consulDependencies = renderer.AnalyzeDependencies(templatePath, properties);
            TemplatePath = templatePath;
            OutputPath = outputPath;
            _properties = properties;

            ConsulState = new ConsulState();
            _subscriptions = ConsulState.ObserveAll(_consulDependencies, client);

            ConsulState.Changes.Subscribe(_ => RenderTemplate());
        }

        private void RenderTemplate()
        {
            if (ConsulState.SatisfiesAll(_consulDependencies))
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
            _subscriptions.Dispose();
        }
    }
}